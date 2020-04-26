using System.Collections.Generic;
using UnityEngine;

namespace Assets
{

    public class ClothVertex
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public Vector3 Force;
        public bool Fixed = false;
        //List<ClothVertex> Neighbors;
    }


    public class KentClothSim : MonoBehaviour
    {
        [Header("Cloth Properties")]
        [SerializeField] [Range(0,1000)] private float SPRING_STIFFNESS_CONSTANT = 1.0f;
        [SerializeField] [Range(0, 20)] private float VERTEX_MASS = 1.0f;
        [SerializeField] private Vector3 GRAVITY_CONSTANT = new Vector3(0.0f,-9.8f,0.0f);
        [SerializeField] [Range(0, 1)] private float GRAVITY_DAMPENING = 0.2f;


        [Header("Spring Properties")]
        [SerializeField] [Range(0, 20)] private float SPRING_REST_LENGTH = 0.5f;

        [SerializeField] [Range(0, 1)] private float SPRING_DAMPENING = 0.2f;

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
                        Position = vertexLocalSpacePosition,
                        Velocity = Vector3.zero
                    };
                    toReturn.Add(new Vector2Int(i, j), xVert);
                }
            }


            toReturn[new Vector2Int(0, 0)].Fixed = true;
            toReturn[new Vector2Int(0, 10)].Fixed = true;
            return toReturn;
        }

        private static List<ClothVertex> GetNeighbors(Dictionary<Vector2Int, ClothVertex> clothStructure,
            Vector2Int targetKey)
        {
            List<ClothVertex> neighbors = new List<ClothVertex>();

            // Loop through neighbors
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    if (i == 0 && j == 0)
                        continue;
                    Vector2Int offset = new Vector2Int(i, j);
                    if (clothStructure.ContainsKey(targetKey + offset))
                    {
                        neighbors.Add(clothStructure[targetKey + offset]);
                    }
                }
            }
            return neighbors;
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


            
        }


        private void FixedUpdate()
        {
            UpdateSimulateCloth(this.clothVertexStructure);

            GetComponent<MeshFilter>().mesh.vertices = ConstructVertexArrayFromDataStructure(this.clothVertexStructure, this.clothResolution);
        }




        private void UpdateSimulateCloth(Dictionary<Vector2Int, ClothVertex> clothStructure)
        {

            float deltaTime = Time.deltaTime;

            foreach (KeyValuePair<Vector2Int, ClothVertex> keyPair in clothStructure)
            {

                Vector2Int key = keyPair.Key;
                ClothVertex currentClothVertex = keyPair.Value;
                Vector3 currentVelocity = currentClothVertex.Velocity;


                


                // Calculate Forces
                {
                    //Gravity
                    Vector3 gravityForce = (GRAVITY_CONSTANT * VERTEX_MASS);
                    currentClothVertex.Force += gravityForce;
                    

                    // TODO Wind

                    // TODO Air Resistance




                    //Spring Forces
                    List<ClothVertex> neighbors = GetNeighbors(clothStructure, key);
                    foreach (ClothVertex neighbor in neighbors)
                    {
                        Vector3 positionVectorToNeighbor = (neighbor.Position -  currentClothVertex.Position);
                        Vector3 springForceDirectionToCenter = positionVectorToNeighbor.normalized;


                        // Longer = Positive = Force Should Go Toward Current Cloth Vertex
                        // Shorter = Negative = Force Should Go Toward Neighbor
                        float force =  this.SPRING_STIFFNESS_CONSTANT * (positionVectorToNeighbor.magnitude - this.SPRING_REST_LENGTH);

                        //force = Mathf.Max(force - (this.SPRING_DAMPENING * force),0.0f);



                        currentClothVertex.Force += springForceDirectionToCenter * force;
                    }
                }


                // Dampening
                currentClothVertex.Force = currentClothVertex.Force - (currentClothVertex.Force * SPRING_DAMPENING);


                // Integrate Velocity
                {
                    currentClothVertex.Velocity = currentClothVertex.Velocity +
                                                  ((currentClothVertex.Force / VERTEX_MASS) * (deltaTime));
                }

                // Integrate Position
                {
                    if (!currentClothVertex.Fixed)
                    {
                        currentClothVertex.Position = currentClothVertex.Position +
                                                      ((currentClothVertex.Velocity) * (deltaTime));
                    }
                    
                }

                //if (keyPair.Key.x % 2 == 0)
                //{
                //    //keyPair.Value.Position.x = Mathf.Sin(Time.time);
                //    //vertices[i] += normals[i] * Mathf.Sin(Time.time);
                //}
            }
        }

        

        private void OnDrawGizmos()
        {

            if (this.clothVertexStructure != null)
            {
                Matrix4x4 transform = this.transform.localToWorldMatrix;

                foreach (KeyValuePair<Vector2Int, ClothVertex> keyPair in this.clothVertexStructure)
                {
                    ClothVertex vert = keyPair.Value;

                    Vector3 start = transform.MultiplyPoint(vert.Position);
                    Gizmos.color = Color.white;
                    Gizmos.DrawSphere(start, 0.1f);


                    Gizmos.color = Color.red;
                    Vector3 end = start + vert.Force.normalized * 1.0f;
                    Gizmos.DrawLine(start, end);
                }



                Vector2Int key = new  Vector2Int(1,1);
                var neighbors = GetNeighbors(this.clothVertexStructure, key);
                var centerVertex = this.clothVertexStructure[key];

                Gizmos.color = Color.yellow;
                
                Gizmos.DrawSphere(transform.MultiplyPoint(centerVertex.Position), 0.1f);

                Gizmos.color = Color.red;
                foreach (var neighbor in neighbors)
                {
                    Gizmos.DrawSphere(transform.MultiplyPoint(neighbor.Position), 0.1f);
                }
            }

            else
            {
                Mesh mesh = GetComponent<MeshFilter>().sharedMesh;
                for (int i = 0; i < mesh.vertices.Length; i++)
                {
                    Vector3 vertex = mesh.vertices[i];

                    vertex = this.transform.localToWorldMatrix.MultiplyPoint(vertex);

                    Gizmos.DrawSphere(vertex, 0.1f);
                }
            }
        }
    }
}
