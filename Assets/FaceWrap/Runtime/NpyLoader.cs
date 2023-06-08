using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

public static class NpyLoader
{
    public static class pyparse
    {
        public static string GetValueFromMap(string mapstr) {
            int sep_pos = mapstr.IndexOf(":");
            if (sep_pos == -1) {
                return "";
            }
            string tmp = mapstr.Substring(sep_pos + 1);
            return tmp.Trim();
        }
        
        /** Parses the string representation of a Python dict The keys need to be known and may not appear anywhere else in the data. */
        public static Dictionary<string, string> ParseDict(string inputString, List<string> keys)
        {
            Dictionary<string, string> map = new Dictionary<string, string>();
            if (keys.Count == 0) return map;
            inputString = inputString.Trim();
            // unwrap dictionary
            if ((inputString.First() == '{') && (inputString.Last() == '}'))
                inputString = inputString.Substring(1, inputString.Length - 2);
            else
                throw new Exception("Not a Python dictionary.");
            List<(int pos, string value)> positions = new List<(int pos, string value)>();
            foreach (var value in keys)
            {
                int pos = inputString.IndexOf("'" + value + "'");
                if (pos == -1)
                    throw new Exception("Missing '" + value + "' key.");
                var position_pair = (pos, value);
                positions.Add(position_pair);
            }
            // sort by position in dict
            positions.Sort();
            for (int i = 0; i < positions.Count; ++i)
            {
                string rawValue;
                int begin = positions[i].pos;
                int end = -1;
                string key = positions[i].value;
                if (i + 1 < positions.Count)
                    end = positions[i + 1].pos;
                else
                    end = inputString.Length;
                
                rawValue = inputString.Substring(begin, end - begin);
                rawValue = rawValue.Trim();
                if (rawValue.Last() == ',')
                    rawValue = rawValue.Remove(rawValue.Length - 1);
                string value = GetValueFromMap(rawValue);
                map.Add(key, value);
            }
            return map;
        }
        public static bool ParseBool(string input)
        {
            if (input == "True")
                return true;
            if (input == "False")
                return false;

            throw new Exception("Invalid python boolan.");
        }

        public static string ParseStr(string input)
        {
            if ((input.First() == '\'') && (input.Last() == '\''))
                return input.Substring(1, input.Length - 2);

            throw new Exception("Invalid python string.");
        }

        public static List<string> ParseTuple(string input)
        {
            char seperator = ',';

            input = input.Trim();

            if ((input.First() == '(') && (input.Last() == ')'))
                input = input.Substring(1, input.Length - 2);
            else
                throw new Exception("Invalid Python tuple.");

            List<string> v = input.Split(seperator).ToList();

            return v;
        }

        public static string WriteTuple<T>(List<T> v)
        {
            if (v.Count == 0)
                return "()";

            StringBuilder sb = new StringBuilder();

            if (v.Count == 1)
            {
                sb.Append("(").Append(v[0]).Append(",)");
            }
            else
            {
                const string delimiter = ", ";
                // v.Count > 1
                sb.Append("(");
                for (int i = 0; i < v.Count - 1; i++)
                    sb.Append(v[i]).Append(delimiter);
                sb.Append(v);
                sb.Append(")");
            }

            return sb.ToString();
        }

        public static string WriteBoolean(bool b)
        {
            if (b)
                return "True";
            else
                return "False";
        }
    }
    
    const bool big_endian = false;
    
    const string magic_string = "\x93NUMPY";
    const int magic_string_length = 6;

    const char little_endian_char = '<';
    const char big_endian_char = '>';
    const char no_endian_char = '|';

    static readonly char[] endian_chars = {little_endian_char, big_endian_char, no_endian_char};
    static readonly char[] numtype_chars = {'f', 'i', 'u', 'c'};

    static readonly char host_endian_char = (big_endian ? big_endian_char : little_endian_char);
    
    static readonly Dictionary<Type, dtype_t> dtype_map = new Dictionary<Type, dtype_t>
    {
        [typeof(float)] = new dtype_t(host_endian_char, 'f', sizeof(float)),
        [typeof(double)] = new dtype_t(host_endian_char, 'f', sizeof(double)),
        [typeof(char)] = new dtype_t(no_endian_char, 'i', sizeof(char)),
        [typeof(sbyte)] = new dtype_t(no_endian_char, 'i', sizeof(sbyte)),
        [typeof(short)] = new dtype_t(host_endian_char, 'i', sizeof(short)),
        [typeof(int)] = new dtype_t(host_endian_char, 'i', sizeof(int)),
        [typeof(long)] = new dtype_t(host_endian_char, 'i', sizeof(long)),
        [typeof(byte)] = new dtype_t(no_endian_char, 'u', sizeof(byte)),
        [typeof(ushort)] = new dtype_t(host_endian_char, 'u', sizeof(ushort)),
        [typeof(uint)] = new dtype_t(host_endian_char, 'u', sizeof(uint)),
        [typeof(ulong)] = new dtype_t(host_endian_char, 'u', sizeof(ulong))
    };
/* npy array length */


    public struct dtype_t 
    {
        public char byteorder;
        public char kind;
        public uint itemsize;

        public dtype_t(char byteorder, char kind, uint itemsize)
        {
            this.byteorder = byteorder;
            this.kind = kind;
            this.itemsize = itemsize;
        }

        // TODO(llohse): implement as constexpr
        public string str() {
            const int max_buflen = 16;
            var buf = new StringBuilder(max_buflen);
            buf.AppendFormat("{0}{1}{2}", byteorder, kind, itemsize);
            return buf.ToString();
        }

        public Tuple<char, char, uint> tie() {
            return Tuple.Create(byteorder, kind, itemsize);
        }
    }

    struct header_t 
    {
        public dtype_t dtype;
        public bool fortran_order;
        public List<int> shape;
    }
    
    public static bool IsDigits(string str) 
    {
        return str.All(char.IsDigit);
    }

    public static bool InArray<T>(T val, T[] arr) 
    {
        return Array.IndexOf(arr, val) != -1;
    }

    public static dtype_t ParseDescr(string typestring) 
    {
        if (typestring.Length < 3) {
            throw new Exception("invalid typestring (length)");
        }

        char byteorder_c = typestring[0];
        char kind_c = typestring[1];
        string itemsize_s = typestring.Substring(2);

        if (!InArray(byteorder_c, endian_chars)) {
            throw new Exception("invalid typestring (byteorder)");
        }

        if (!InArray(kind_c, numtype_chars)) {
            throw new Exception("invalid typestring (kind)");
        }

        if (!IsDigits(itemsize_s)) {
            throw new Exception("invalid typestring (itemsize)");
        }
        uint itemsize = uint.Parse(itemsize_s);

        return new dtype_t {byteorder = byteorder_c, kind = kind_c, itemsize = itemsize};
    }
    
    private static Tuple<byte, byte> ReadMagic(Stream istream) 
    {
        byte[] buf = new byte[magic_string_length + 2];
        istream.Read(buf, 0, magic_string_length + 2);

        for (int i = 0; i < magic_string_length; i++)
        {
            if (buf[i] != magic_string[i])
            {
                throw new Exception("this file does not have a valid npy format.");
            }
        }

        Tuple<byte, byte> version = new Tuple<byte, byte>(buf[magic_string_length], buf[magic_string_length + 1]);

        return version;
    }
    
    private static header_t ParseHeader(string header)
    {
        if (header.Last() != '\n')
            throw new Exception("invalid header");
        header = header.Remove(header.Length - 1);

        var keys = new List<string> { "descr", "fortran_order", "shape" };
        var dictMap = pyparse.ParseDict(header, keys);

        if (dictMap.Count == 0)
            throw new Exception("invalid dictionary in header");

        var descrS = dictMap["descr"];
        var fortranS = dictMap["fortran_order"];
        var shapeS = dictMap["shape"];

        var descr = pyparse.ParseStr(descrS);
        var dtype = ParseDescr(descr);

        bool fortranOrder = pyparse.ParseBool(fortranS);

        var shapeV = pyparse.ParseTuple(shapeS);

        var shape = new List<int>();
        foreach (var item in shapeV)
        {
            int dim = int.Parse(item);
            shape.Add(dim);
        }

        return new header_t{dtype = dtype, fortran_order = fortranOrder, shape = shape};
    }
    
    public static string ReadHeader(Stream stream)
    {
        var version = ReadMagic(stream);
        uint headerLength;
        if (version.Item1 == 1 && version.Item2 == 0)
        {
            var headerLenLe16 = new byte[2];
            stream.Read(headerLenLe16, 0, 2);
            headerLength = (uint)((headerLenLe16[0] << 0) | (headerLenLe16[1] << 8));
            if ((magic_string_length + 2 + 2 + headerLength) % 16 != 0)
            {
                // TODO(llohse): display warning
            }
        }
        else if (version.Item1 == 2 && version.Item2 == 0)
        {
            var headerLenLe32 = new byte[4];
            stream.Read(headerLenLe32, 0, 4);
            headerLength = (uint)((headerLenLe32[0] << 0) | (headerLenLe32[1] << 8) | (headerLenLe32[2] << 16) | (headerLenLe32[3] << 24));
            if ((magic_string_length + 2 + 4 + headerLength) % 16 != 0)
            {
                // TODO(llohse): display warning
            }
        }
        else
        {
            throw new NotSupportedException("unsupported file format version");
        }

        var bufV = new byte[headerLength];
        stream.Read(bufV, 0, (int)headerLength);
        return Encoding.UTF8.GetString(bufV);
    }
    
    public static int CompSize(List<int> shape)
    {
        int size = 1;
        foreach (int i in shape)
            size *= i;

        return size;
    }
    
    public static void LoadArrayFromNumpy<Scalar>(string filename, out List<int> shape, out bool fortran_order,
        out Scalar[] data) {
        using (var stream = new FileStream(filename, FileMode.Open)) 
        {
            var header_s = ReadHeader(stream);

            // parse header
            var header = ParseHeader(header_s);

            // check if the typestring matches the given one
//  static_assert(has_typestring<Scalar>::value, "scalar type not understood");
            var dtype = dtype_map[typeof(Scalar)];

            if (!header.dtype.tie().Equals(dtype.tie()) ) {
                throw new Exception("formatting error: typestrings not matching");
            }

            shape = header.shape.ToList();
            fortran_order = header.fortran_order;

            // compute the data size based on the shape
            var size = (int)CompSize(shape);
            data = new Scalar[size];

            // read the data
            var buffer = new byte[size * dtype.itemsize];
            stream.Read(buffer, 0, buffer.Length);
            Buffer.BlockCopy(buffer, 0, data, 0, buffer.Length);
            
        }
    }
}