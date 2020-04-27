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
        public List<ClothVertex> StructuralNeighbors = new List<ClothVertex>();// 1 away, Up Down Left Right
        public List<ClothVertex> ShearNeighbors = new List<ClothVertex>();// Diagnols
        public List<ClothVertex> FlexionNeighbors = new List<ClothVertex>();// 2 away, Up Down Left Right
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
        [SerializeField] [Range(0, 1)] private float FORCE_DAMPENING = 0.5f;
        [SerializeField] [Range(0, 1)] private float VELOCITY_DAMPENING = 0.5f;

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

            // Set Anchor Points
            toReturn[new Vector2Int(0, 0)].Fixed = true;
            toReturn[new Vector2Int(0, 10)].Fixed = true;


            // Construct Neighbor Lists
            for (int i = 0; i < resolution.x; i++)
            {
                for (int j = 0; j < resolution.y; j++)
                {
                    Vector2Int key =  new Vector2Int(i, j);

                    GetMassSpringNeighbors(toReturn, key, 
                        out toReturn[key].StructuralNeighbors,
                        out toReturn[key].ShearNeighbors, 
                        out toReturn[key].FlexionNeighbors);
                }
            }

            return toReturn;
        }

        private static void GetMassSpringNeighbors(Dictionary<Vector2Int, ClothVertex> clothStructure, Vector2Int targetKey, out List<ClothVertex> o_structuralNeighbors, out List<ClothVertex> o_shearNeighbors, out List<ClothVertex> o_flexionNeighbors)
        {
            o_structuralNeighbors = new List<ClothVertex>();
            o_shearNeighbors = new List<ClothVertex>();
            o_flexionNeighbors = new List<ClothVertex>();

            // Loop through neighbors and get shear and structural
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    if (i == 0 && j == 0)
                        continue;
                    Vector2Int offset = new Vector2Int(i, j);
                    if (clothStructure.ContainsKey(targetKey + offset))
                    {

                        if (Mathf.Abs(i) != Mathf.Abs(j))
                        {
                            o_structuralNeighbors.Add(clothStructure[targetKey + offset]);
                        }
                        else
                        {
                            o_shearNeighbors.Add(clothStructure[targetKey + offset]);
                        }
                        
                    }
                }
            }

            // Get Flexion
            {
                Vector2Int offset;
                offset = new Vector2Int(0, 2);
                if (clothStructure.ContainsKey(targetKey + offset))
                    o_flexionNeighbors.Add(clothStructure[targetKey + offset]);
                offset = new Vector2Int(0, -2);
                if (clothStructure.ContainsKey(targetKey + offset))
                    o_flexionNeighbors.Add(clothStructure[targetKey + offset]);
                offset = new Vector2Int(2, 0);
                if (clothStructure.ContainsKey(targetKey + offset))
                    o_flexionNeighbors.Add(clothStructure[targetKey + offset]);
                offset = new Vector2Int(-2, 0);
                if (clothStructure.ContainsKey(targetKey + offset))
                    o_flexionNeighbors.Add(clothStructure[targetKey + offset]);
            }

            return;
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

        private static Vector3 CalculateAdditiveSpringForce(Vector3 positionCenter, Vector3 positionNeighbor, float springStiffnessConstant, float springRestLength)
        {
            Vector3 positionVectorToCenter = (positionCenter - positionNeighbor);
            //Vector3 velocityVectorToCenter = (currentClothVertex.Velocity - neighbor.Velocity);

            Vector3 springForceDirectionToCenter = positionVectorToCenter.normalized;
            float currentLength = positionVectorToCenter.magnitude;

            // Longer = Positive = Force Should Go Toward Current Cloth Vertex
            // Shorter = Negative = Force Should Go Toward Neighbor
            float springForce = springStiffnessConstant * (springRestLength - currentLength);
            //float dampening = SPRING_DAMPENING *
            //                  (Vector3.Dot(velocityVectorToCenter, positionVectorToCenter) / currentLength);



            //force = Mathf.Max(force - (this.SPRING_DAMPENING * force),0.0f);
            return springForceDirectionToCenter * (springForce);
        }

        

        // Update is called once per frame
        void Update()
        {

            
        }


        private void FixedUpdate()
        {
            UpdateSimulateCloth(this.clothVertexStructure);

            GetComponent<MeshFilter>().mesh.vertices = ConstructVertexArrayFromDataStructure(this.clothVertexStructure, this.clothResolution);
            GetComponent<MeshFilter>().mesh.RecalculateNormals();
            GetComponent<MeshFilter>().mesh.RecalculateTangents();
        }




        private void UpdateSimulateCloth(Dictionary<Vector2Int, ClothVertex> clothStructure)
        {

            float deltaTime = Time.fixedDeltaTime;

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
                    foreach (ClothVertex neighbor in currentClothVertex.StructuralNeighbors)
                    {
                        currentClothVertex.Force += CalculateAdditiveSpringForce(currentClothVertex.Position, neighbor.Position,
                            this.SPRING_STIFFNESS_CONSTANT, this.SPRING_REST_LENGTH);
                    }
                    foreach (ClothVertex neighbor in currentClothVertex.ShearNeighbors)
                    {
                        currentClothVertex.Force += CalculateAdditiveSpringForce(currentClothVertex.Position, neighbor.Position,
                            this.SPRING_STIFFNESS_CONSTANT, this.SPRING_REST_LENGTH);
                    }
                    foreach (ClothVertex neighbor in currentClothVertex.FlexionNeighbors)
                    {
                        currentClothVertex.Force += CalculateAdditiveSpringForce(currentClothVertex.Position, neighbor.Position,
                            this.SPRING_STIFFNESS_CONSTANT, this.SPRING_REST_LENGTH);
                    }
                }


                // Dampening All Forces
                currentClothVertex.Force = currentClothVertex.Force - (currentClothVertex.Force * FORCE_DAMPENING);


                // Integrate Velocity
                {
                    currentClothVertex.Velocity = currentClothVertex.Velocity +
                                                  ((currentClothVertex.Force / VERTEX_MASS) * (deltaTime));

                    // Dampening Velocity
                    currentClothVertex.Velocity =
                        currentClothVertex.Velocity - (currentClothVertex.Velocity * VELOCITY_DAMPENING);

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



                Vector2Int key = new  Vector2Int(2,2);
                List<ClothVertex> structuralNeighbors;
                List<ClothVertex> shearNeighbors;
                List<ClothVertex> flexiconNeighbors;
                GetMassSpringNeighbors(this.clothVertexStructure, key, out structuralNeighbors, out shearNeighbors, out flexiconNeighbors);

                // Color Center
                {
                    var centerVertex = this.clothVertexStructure[key];
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawSphere(transform.MultiplyPoint(centerVertex.Position), 0.1f);
                }


                // Color neighbors
                {
                    Gizmos.color = Color.red;
                    foreach (var neighbor in structuralNeighbors)
                    {
                        Gizmos.DrawSphere(transform.MultiplyPoint(neighbor.Position), 0.1f);
                    }

                    Gizmos.color = Color.green;
                    foreach (var neighbor in shearNeighbors)
                    {
                        Gizmos.DrawSphere(transform.MultiplyPoint(neighbor.Position), 0.1f);
                    }

                    Gizmos.color = Color.blue;
                    foreach (var neighbor in flexiconNeighbors)
                    {
                        Gizmos.DrawSphere(transform.MultiplyPoint(neighbor.Position), 0.1f);
                    }
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
