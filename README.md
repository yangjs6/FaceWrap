# FaceWrap
在传统标识关键点和传输算法的过程中，除了考虑模型的顶点位置信息外，还加入了顶点的 UV 信息。
即在标识关键点时，记录了顶点的 空间三维位置（x,y,z）和二维纹理映射坐标（u,v）。在顶点传输算法中，每个待传输的顶点，在查找最临近关键点时，使用了以上五维向量数据，并计算传输过程中的权重。

![image](https://github.com/yangjs6/FaceWrap/assets/16881218/7720418d-a9b4-42db-8ae5-8e9df2f13e36)

![image](https://github.com/yangjs6/FaceWrap/assets/16881218/94de4c77-8e57-46e7-8214-053433e5cd5d)


口型顶点动画说明：
已知 模型 A 的静态模型 VertexA0，以及 口型顶点动画每一帧的 每个顶点的变化数据 DeltaVertexA，希望在 另一个已知 模型 B 的静态模型 VertexB0 上，应用该口型动画，即计算每一帧的每个顶点的变化数据 DeltaVertexB

https://github.com/yangjs6/FaceWrap/assets/16881218/9dd93a0b-5efc-4089-bc4d-179ffacc0e68

