using System.Collections.Generic;
using UnityEngine;

namespace Assets
{

    public class ClothVertex
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public Vector3 Forces;
        //List<ClothVertex> Neighbors;
    }


    public class KentClothSim : MonoBehaviour
    {
        [Header("Cloth Properties")]
        [SerializeField] private float SpringStiffness;
        [SerializeField] private float PointMass;
        [SerializeField] private Vector3 GravityConstant = new Vector3(0.0f,-9.8f,0.0f);
        

        private Dictionary<Vector2Int, ClothVertex> clothVertexStructure;
        private Vector2Int clothResolution = new Vector2Int(11,11);// Hardcoded due to used plane mesh


        // Cached values
        

        private void Awake()
        {
            
        }

        // Start is called before the first frame update
        void Start()
        {
            clothVertexStructure = ConstructClothMeshDataStructure(GetComponent<MeshFilter>().mesh, clothResolution);

            
            
        }


        private static Dictionary<Vector2Int, ClothVertex> ConstructClothMeshDataStructure(Mesh givenMesh,Vector2Int resolution)
        {
            Vector3[] vertices = givenMesh.vertices;

            // Construct Dictionary
            Dictionary<Vector2Int, ClothVertex> toReturn = new Dictionary<Vector2Int, ClothVertex>();
            for (int i = 0; i < resolution.x; i++)
            {
                for (int j = 0; j < resolution.y; j++)
                {
                    Vector3 vertexLocalSpacePosition = vertices[(i * resolution.x) + j];

                    ClothVertex xVert = new ClothVertex()
                    {
                        Position = vertexLocalSpacePosition
                    };
                    toReturn.Add(new Vector2Int(i, j), xVert);
                }
            }


            return toReturn;
        }


        private static Vector3[] ConstructVertexArrayFromDataStructure(Dictionary<Vector2Int, ClothVertex> clothStructure, Vector2Int resolution)
        {
            Vector3[] toReturn = new Vector3[clothStructure.Count];

            for (int i = 0; i < resolution.x; i++)
            {
                for (int j = 0; j < resolution.y; j++)
                {
                    Vector2Int key = new Vector2Int(i, j);
                    toReturn[(i * resolution.x) + j] = clothStructure[key].Position;
                }
            }


            return toReturn;
        }

        //private static List<ClothVertex> GetNeighbors(Dictionary<Vector2Int, ClothVertex> structure, )

        // Update is called once per frame
        void Update()
        {
            //Vector3[] vertices = this.clothMesh.vertices;
            //Vector3[] normals = clothMesh.normals;
            //// Modify Vertixes
            //for (var i = 0; i < vertices.Length; i++)
            //{
            //    if ( i % 2 == 0)
            //    {
            //        //vertices[i] += normals[i] * Mathf.Sin(Time.time);
            //    }
                
            //}


            UpdateSimulateCloth(this.clothVertexStructure);

            GetComponent<MeshFilter>().mesh.vertices = ConstructVertexArrayFromDataStructure(this.clothVertexStructure,this.clothResolution);
        }


        private void FixedUpdate()
        {
            
        }




        private void UpdateSimulateCloth(Dictionary<Vector2Int, ClothVertex> clothStructure)
        {
            foreach (KeyValuePair<Vector2Int, ClothVertex> keyPair in clothStructure)
            {

                if (keyPair.Key.x % 2 == 0)
                {
                    //keyPair.Value.Position.x = Mathf.Sin(Time.time);
                    //vertices[i] += normals[i] * Mathf.Sin(Time.time);
                }


            }
        }

        

        private void OnDrawGizmos()
        {
            
            Mesh mesh = GetComponent<MeshFilter>().sharedMesh;
            for ( int i = 0 ; i < mesh.vertices.Length; i++)
            {
                Vector3 vertex = mesh.vertices[i];

                vertex = this.transform.localToWorldMatrix.MultiplyPoint(vertex);

                Gizmos.DrawSphere(vertex, 0.1f);
            }
        }
    }
}
