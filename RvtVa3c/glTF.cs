using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RvtVa3c.Va3cContainer;

namespace RvtVa3c
{
    public class glTFBinaryData
    {
        public List<float> vertexBuffer { get; set; } = new List<float>();
        public List<int> indexBuffer { get; set; } = new List<int>();
        public List<float> normalBuffer { get; set; } = new List<float>();
        public int vertexAccessorIndex { get; set; }
        public int indexAccessorIndex { get; set; }
        public int normalsAccessorIndex { get; set; }
        public string name { get; set; }
    }
    public class glTFVersion
    {
        public string version = "2.0";
    }
    public class glTF
    {
        public glTFVersion asset = new glTFVersion();

        public List<glTFScenes> scenes = new List<glTFScenes>() { new glTFScenes() };
        public List<glTFNode> nodes { get; set; } = new List<glTFNode>();
        public List<glTFMesh> meshes { get; set; } = new List<glTFMesh>();
        public List<glTFBuffer> buffers { get; set; } = new List<glTFBuffer>();
        public List<glTFBufferView> bufferViews { get; set; } = new List<glTFBufferView>();
        public List<glTFAccessor> accessors { get; set; } = new List<glTFAccessor>();

        public int AddChildNode (glTFNode node, int parentIndex)
        {
            this.nodes.Add(node);
            this.nodes[parentIndex].children.Add(this.nodes.Count - 1);
            return this.nodes.Count - 1;
        }

        public int AddMesh (glTFMesh mesh)
        {
            this.meshes.Add(mesh);
            return this.meshes.Count - 1;
        }

        private float[] getVec3MinMax(List<float> vec3)
        {
            float minVertexX = float.MaxValue;
            float minVertexY = float.MaxValue;
            float minVertexZ = float.MaxValue;
            float maxVertexX = float.MinValue;
            float maxVertexY = float.MinValue;
            float maxVertexZ = float.MinValue;
            for (int i = 0; i < (vec3.Count / 3); i += 3)
            {
                float currentMinX = Math.Min(minVertexX, vec3[i]);
                float currentMaxX = Math.Max(maxVertexX, vec3[i]);
                if (currentMinX < minVertexX) minVertexX = currentMinX;
                if (currentMaxX > maxVertexX) maxVertexX = currentMaxX;

                float currentMinY = Math.Min(minVertexY, vec3[i + 1]);
                float currentMaxY = Math.Max(maxVertexY, vec3[i + 1]);
                if (currentMinY < minVertexY) minVertexY = currentMinY;
                if (currentMaxY > maxVertexY) maxVertexY = currentMaxY;

                float currentMinZ = Math.Min(minVertexZ, vec3[i + 2]);
                float currentMaxZ = Math.Max(maxVertexZ, vec3[i + 2]);
                if (currentMinZ < minVertexZ) minVertexZ = currentMinZ;
                if (currentMaxZ > maxVertexZ) maxVertexZ = currentMaxZ;
            }
            return new float[] { minVertexX, maxVertexX, minVertexY, maxVertexY, minVertexZ, maxVertexZ };
        }

        private int[] getScalarMinMax(List<int> scalars)
        {
            int minFaceIndex = int.MaxValue;
            int maxFaceIndex = int.MinValue;
            for (int i = 0; i < scalars.Count; i++)
            {
                int currentMin = Math.Min(minFaceIndex, scalars[i]);
                if (currentMin < minFaceIndex) minFaceIndex = currentMin;

                int currentMax = Math.Max(maxFaceIndex, scalars[i]);
                if (currentMax > maxFaceIndex) maxFaceIndex = currentMax;
            }
            return new int[] { minFaceIndex, maxFaceIndex };
        }

        public glTFBinaryData AddGeometryMeta(Va3cGeometry geom, string name)
        {
            // add a buffer
            glTFBuffer buffer = new glTFBuffer();
            buffer.uri = name + ".bin";
            this.buffers.Add(buffer);
            int bufferIdx = this.buffers.Count - 1;

            Va3cGeometryData geomData = geom.data;
            glTFBinaryData bufferData = new glTFBinaryData();
            bufferData.name = buffer.uri;
            foreach (var coord in geomData.vertices)
            {
                bufferData.vertexBuffer.Add((float)coord);
            }
            foreach (var index in geomData.faces)
            {
                bufferData.indexBuffer.Add(index);
            }
            foreach (var normal in geomData.normals)
            {
                bufferData.normalBuffer.Add((float)normal);
            }

            // Get max and min for vertex data
            float[] vertexMinMax = getVec3MinMax(bufferData.vertexBuffer);

            // Get max and min for normal data
            float[] normalMinMax = getVec3MinMax(bufferData.normalBuffer);

            // Get max and min for index data
            int[] faceMinMax = getScalarMinMax(bufferData.indexBuffer);

            // Add a vec3 buffer view
            int elementsPerVertex = 3;
            int bytesPerElement = 4;
            int bytesPerVertex = elementsPerVertex * bytesPerElement;
            int numVec3 = (geom.data.normals.Count + geom.data.vertices.Count) / elementsPerVertex;
            int sizeOfVec3View = numVec3 * bytesPerVertex;
            glTFBufferView vec3View = new glTFBufferView();
            vec3View.buffer = bufferIdx;
            vec3View.byteOffset = 0;
            vec3View.byteLength = sizeOfVec3View;
            vec3View.target = Targets.ARRAY_BUFFER;
            this.bufferViews.Add(vec3View);
            int vec3ViewIdx = this.bufferViews.Count - 1;

            // add a position buffer view
            //int elementsPerVertex = 3;
            //int bytesPerElement = 4;
            //int bytesPerVertex = elementsPerVertex * bytesPerElement;
            //int numVertices = geom.data.vertices.Count / elementsPerVertex;
            //int sizeOfView = numVertices * bytesPerVertex;
            //glTFBufferView positionView = new glTFBufferView();
            //positionView.buffer = bufferIdx;
            //positionView.byteOffset = 0;
            //positionView.byteLength = sizeOfView;
            //positionView.target = Targets.ARRAY_BUFFER;
            //this.bufferViews.Add(positionView);
            //int positionViewIdx = this.bufferViews.Count - 1;

            // Add a faces / indexes buffer view
            int elementsPerIndex = 1;
            int bytesPerIndexElement = 4;
            int bytesPerIndex = elementsPerIndex * bytesPerIndexElement;
            int numIndexes = geom.data.faces.Count;
            int sizeOfIndexView = numIndexes * bytesPerIndex;
            glTFBufferView facesView = new glTFBufferView();
            facesView.buffer = bufferIdx;
            facesView.byteOffset = vec3View.byteLength;
            facesView.byteLength = sizeOfIndexView;
            facesView.target = Targets.ELEMENT_ARRAY_BUFFER;
            this.bufferViews.Add(facesView);
            int facesViewIdx = this.bufferViews.Count - 1;

            this.buffers[bufferIdx].byteLength = vec3View.byteLength + facesView.byteLength;

            // add a position accessor
            glTFAccessor positionAccessor = new glTFAccessor();
            positionAccessor.bufferView = vec3ViewIdx;
            positionAccessor.byteOffset = 0;
            positionAccessor.componentType = ComponentType.FLOAT;
            positionAccessor.count = geom.data.vertices.Count / elementsPerVertex;
            positionAccessor.type = "VEC3";
            positionAccessor.max = new List<float>() { vertexMinMax[1], vertexMinMax[3], vertexMinMax[5] };
            positionAccessor.min = new List<float>() { vertexMinMax[0], vertexMinMax[2], vertexMinMax[4] };
            this.accessors.Add(positionAccessor);
            bufferData.vertexAccessorIndex = this.accessors.Count - 1;

            // add a normals accessor
            glTFAccessor normalsAccessor = new glTFAccessor();
            normalsAccessor.bufferView = vec3ViewIdx;
            normalsAccessor.byteOffset = (positionAccessor.count) * bytesPerVertex;
            normalsAccessor.componentType = ComponentType.FLOAT;
            normalsAccessor.count = geom.data.normals.Count / elementsPerVertex;
            normalsAccessor.type = "VEC3";
            normalsAccessor.max = new List<float>() { normalMinMax[1], normalMinMax[3], normalMinMax[5] };
            normalsAccessor.min = new List<float>() { normalMinMax[0], normalMinMax[2], normalMinMax[4] };
            this.accessors.Add(normalsAccessor);
            bufferData.normalsAccessorIndex = this.accessors.Count - 1;

            // add a face accessor
            glTFAccessor faceAccessor = new glTFAccessor();
            faceAccessor.bufferView = facesViewIdx;
            faceAccessor.byteOffset = 0;
            faceAccessor.componentType = ComponentType.UNSIGNED_INT;
            faceAccessor.count = numIndexes;
            faceAccessor.type = "SCALAR";
            faceAccessor.max = new List<float>() { faceMinMax[1] };
            faceAccessor.min = new List<float>() { faceMinMax[0] };
            this.accessors.Add(faceAccessor);
            bufferData.indexAccessorIndex = this.accessors.Count - 1;

            return bufferData;
        }
    }
    public class glTFScenes
    {
        public List<int> nodes = new List<int>() { 0 };
    }
    public class glTFMesh
    {
        public List<glTFMeshPrimitive> primitives { get; set; }
    }

    public class glTFAttribute
    {
        /// <summary>
        /// The index of the accessor for position data.
        /// </summary>
        public int POSITION { get; set; }
        public int NORMAL { get; set; }
    }

    public class glTFMeshPrimitive
    {
        public glTFAttribute attributes { get; set; } = new glTFAttribute();
        public int indices { get; set; }
        public int? material { get; set; } = null;
        public int mode { get; set; } = 4; // 4 is triangles
    }

    public class glTFBuffer
    {
        /// <summary>
        /// The uri of the buffer.
        /// </summary>
        public string uri { get; set; }
        /// <summary>
        /// The total byte length of the buffer.
        /// </summary>
        public int byteLength { get; set; }
    }

    public enum Targets
    {
        ARRAY_BUFFER = 34962,
        ELEMENT_ARRAY_BUFFER = 34963
    }

    public class glTFBufferView
    {
        /// <summary>
        /// The index of the buffer.
        /// </summary>
        public int buffer { get; set; }
        /// <summary>
        /// The offset into the buffer in bytes.
        /// </summary>
        public int byteOffset { get; set; }
        /// <summary>
        /// The length of the bufferView in bytes.
        /// </summary>
        public int byteLength { get; set; }
        /// <summary>
        /// The target that the GPU buffer should be bound to.
        /// </summary>
        public Targets target { get; set; }
        /// <summary>
        /// A user defined name for this view.
        /// </summary>
        public string name { get; set; }
    }

    public class glTFAccessor
    {
        /// <summary>
        /// The index of the bufferView.
        /// </summary>
        public int bufferView { get; set; }
        /// <summary>
        /// The offset relative to the start of the bufferView in bytes.
        /// </summary>
        public int byteOffset { get; set; }
        /// <summary>
        /// the datatype of the components in the attribute
        /// </summary>
        public ComponentType componentType { get; set; }
        /// <summary>
        /// The number of attributes referenced by this accessor.
        /// </summary>
        public int count { get; set; }
        /// <summary>
        /// Specifies if the attribute is a scala, vector, or matrix
        /// </summary>
        public string type { get; set; }
        /// <summary>
        /// Maximum value of each component in this attribute.
        /// </summary>
        public List<float> max { get; set; }
        /// <summary>
        /// Minimum value of each component in this attribute.
        /// </summary>
        public List<float> min { get; set; }
        /// <summary>
        /// A user defined name for this accessor.
        /// </summary>
        public string name { get; set; }
    }

    public enum ComponentType
    {
        BYTE = 5120,
        UNSIGNED_BYTE = 5121,
        SHORT = 5122,
        UNSIGNED_SHORT = 5123,
        UNSIGNED_INT = 5125,
        FLOAT = 5126
    }

    public class glTFNode
    {
        /// <summary>
        /// The user-defined name of this object
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// The index of the mesh in this node.
        /// </summary>
        public int? mesh { get; set; } = null;
        /// <summary>
        /// A floating-point 4x4 transformation matrix stored in column major order.
        /// </summary>
        public List<float> matrix { get; set; } = new List<float>();
        /// <summary>
        /// The indices of this node's children.
        /// </summary>
        public List<int> children { get; set; } = new List<int>();
    }
}
