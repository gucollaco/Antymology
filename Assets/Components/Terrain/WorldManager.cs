using Antymology.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Antymology.Terrain
{
    public class WorldManager : Singleton<WorldManager>
    {
        #region Fields

        /// <summary>
        /// The generation the simulation is at.
        /// </summary>
        public int generation;

        /// <summary>
        /// The turn the current generation is at.
        /// </summary>
        public int turn;

        /// <summary>
        /// Currently alive ants.
        /// </summary>
        public List<GameObject> currentAnts;

        /// <summary>
        /// Current queen.
        /// </summary>
        public GameObject currentQueen;

        /// <summary>
        /// The ant prefab.
        /// </summary>
        public GameObject antPrefab;

        /// <summary>
        /// The queen prefab.
        /// </summary>
        public GameObject queenPrefab;

        /// <summary>
        /// The material used for eech block.
        /// </summary>
        public Material blockMaterial;

        /// <summary>
        /// The generation text.
        /// </summary>
        public TextMeshProUGUI textGeneration;

        /// <summary>
        /// The turn text.
        /// </summary>
        public TextMeshProUGUI textTurn;

        /// <summary>
        /// The nest blocks text.
        /// </summary>
        public TextMeshProUGUI textNestBlocks;

        /// <summary>
        /// The raw data of the underlying world structure.
        /// </summary>
        private AbstractBlock[,,] Blocks;

        /// <summary>
        /// Reference to the geometry data of the chunks.
        /// </summary>
        private Chunk[,,] Chunks;

        /// <summary>
        /// Random number generator.
        /// </summary>
        private System.Random RNG;

        /// <summary>
        /// Random number generator.
        /// </summary>
        private SimplexNoise SimplexNoise;

        /// <summary>
        /// The ants parent element on the structure hierarchy.
        /// </summary>
        private Transform antsParentTransform;

        /// <summary>
        /// Value to wait after the creation has happened.
        /// </summary>
        private float postCreationWaitValue = 3.0f;

        /// <summary>
        /// Value to wait between each ant action.
        /// </summary>
        private float actionWaitValue = 1.0f;

        /// <summary>
        /// Wait after the creation has happened.
        /// </summary>
        private WaitForSeconds postCreationWait;

        /// <summary>
        /// Wait between each ant action.
        /// </summary>
        private WaitForSeconds actionWait;

        #endregion

        #region Initialization

        /// <summary>
        /// Awake is called before any start method is called.
        /// </summary>
        void Awake()
        {
            // Generate new random number generator
            RNG = new System.Random(ConfigurationManager.Instance.Seed);

            // Generate new simplex noise generator
            SimplexNoise = new SimplexNoise(ConfigurationManager.Instance.Seed);

            // Initialize a new 3D array of blocks with size of the number of chunks times the size of each chunk
            Blocks = new AbstractBlock[
                ConfigurationManager.Instance.World_Diameter * ConfigurationManager.Instance.Chunk_Diameter,
                ConfigurationManager.Instance.World_Height * ConfigurationManager.Instance.Chunk_Diameter,
                ConfigurationManager.Instance.World_Diameter * ConfigurationManager.Instance.Chunk_Diameter];

            // Initialize a new 3D array of chunks with size of the number of chunks
            Chunks = new Chunk[
                ConfigurationManager.Instance.World_Diameter,
                ConfigurationManager.Instance.World_Height,
                ConfigurationManager.Instance.World_Diameter];
            
            // Initialize the currentAnts list.
            currentAnts = new List<GameObject>();

            // Initializing some varialbes.
            generation = 0;
            turn = 0;
        }

        /// <summary>
        /// Called after every awake has been called.
        /// </summary>
        private void Start()
        {
            GameObject antsParentElement = new GameObject("Ants");
            antsParentTransform = antsParentElement.GetComponent<Transform>();
            
            postCreationWait = new WaitForSeconds(postCreationWaitValue);
            actionWait = new WaitForSeconds(actionWaitValue);

            StartCoroutine(SimulationLoop());
        }

        /// <summary>
        /// The simulation loop routine.
        /// </summary>
        private IEnumerator SimulationLoop()
        {
            yield return StartCoroutine(GenerationCreation());
            yield return StartCoroutine(GenerationLiving());

            Debug.Log("Queen died");
            // StartCoroutine(SimulationLoop());
        }

        /// <summary>
        /// The section which creates the generation elements.
        /// </summary>
        private IEnumerator GenerationCreation()
        {
            generation++;

            GenerateData();
            GenerateChunks();
            GenerateQueen();
            GenerateAnts();
            CameraInitialSetup();

            yield return postCreationWait;
        }

        /// <summary>
        /// The section which simulates the ant generation living their lives.
        /// </summary>
        private IEnumerator GenerationLiving()
        {
            while(IsQueenAlive())
            {
                turn++;
                RemoveInactive();
                AntsChooseAction();
                UpdateTexts();
                yield return actionWait;
            }
        }

        /// <summary>
        /// Checks if the queen is alive.
        /// </summary>
        private bool IsQueenAlive()
        {
            Queen queenScript = currentQueen.GetComponent<Queen>();
            return queenScript.health > 0;
        }

        /// <summary>
        /// Update the texts being displayed.
        /// </summary>
        private void RemoveInactive()
        {
            foreach (GameObject ant in currentAnts.ToArray())
            {
                if (!ant.activeSelf)
                    currentAnts.Remove(ant);
            }
        }

        /// <summary>
        /// Update the texts being displayed.
        /// </summary>
        private void UpdateTexts()
        {
            Queen queenScript = currentQueen.GetComponent<Queen>();
            textGeneration.text = $"Generation: {generation}";
            textTurn.text = $"Turn: {turn}";
            textNestBlocks.text = $"Nest blocks: {queenScript.nestsProduced}";
        }

        /// <summary>
        /// Each ant chooses the action to take (the queen also takes her action).
        /// </summary>
        private void AntsChooseAction()
        {
            GameObject[] currentAntsArray = currentAnts.ToArray();
            GameObject lowestHealthAnt = GetAntWithLowestHealth(currentAntsArray);

            Queen queenScript = currentQueen.GetComponent<Queen>();
            queenScript.ChooseAction(lowestHealthAnt);

            foreach (GameObject ant in currentAntsArray)
            {
                Ant antScript = ant.GetComponent<Ant>();
                antScript.ChooseAction(lowestHealthAnt);
            }
        }

        /// <summary>
        /// Method to get the ant with the lowest remaining health.
        /// </summary>
        public GameObject GetAntWithLowestHealth(GameObject[] currentAntsArray)
        {
            // Get the element with the lowest health compared to the total.
            GameObject lowestHealthObject = null;
            float lowestHealth = 10000;
            
            foreach (GameObject antObject in currentAntsArray)
            {
                Ant ant = antObject.GetComponent<Ant>();

                float currentAntHealth = ant.health;
                if (currentAntHealth < lowestHealth)
                {
                    lowestHealthObject = antObject;
                    lowestHealth = currentAntHealth;
                }
            }

            return lowestHealthObject;
        }

        /// <summary>
        /// Set's up the camera initial positioning and look at.
        /// </summary>
        private void CameraInitialSetup()
        {
            Camera.main.transform.position = new Vector3(0 / 2, Blocks.GetLength(1), 0);
            Camera.main.transform.LookAt(new Vector3(Blocks.GetLength(0), 0, Blocks.GetLength(2)));
        }

        /// <summary>
        /// Generating a queen ant.
        /// </summary>
        private void GenerateQueen()
        {
            // Instantiating the unique queen ant.
            int xPosition = GetSpawnXPosition();
            int zPosition = GetSpawnZPosition();
            
            // Loops in case position already has an ant.
            while (HasAntAtPosition(xPosition, zPosition))
            {
                xPosition = GetSpawnXPosition();
                zPosition = GetSpawnZPosition();
            }

            float yPosition = GetSpawnYPosition(xPosition, zPosition);

            Vector3 spawnPosition = new Vector3((float) xPosition, yPosition, (float) zPosition);
            currentQueen = GameObject.Instantiate(queenPrefab, spawnPosition, antPrefab.transform.rotation);
            currentQueen.name = "Queen";
            currentQueen.transform.SetParent(antsParentTransform);
        }
    
        /// <summary>
        /// Generating a non queen ant population.
        /// </summary>
        private void GenerateAnts()
        {
            // Instantiating the initial ants.
            for (int i = 0; i < ConfigurationManager.Instance.InitialPopulation; i++)
            {
                int xPosition = GetSpawnXPosition();
                int zPosition = GetSpawnZPosition();

                // Loops in case position already has an ant.
                while (HasAntAtPosition(xPosition, zPosition))
                {
                    xPosition = GetSpawnXPosition();
                    zPosition = GetSpawnZPosition();
                }

                float yPosition = GetSpawnYPosition(xPosition, zPosition);

                Vector3 spawnPosition = new Vector3((float) xPosition, yPosition, (float) zPosition);
                GameObject createdAnt = GameObject.Instantiate(antPrefab, spawnPosition, antPrefab.transform.rotation);
                createdAnt.name = $"Ant {i}";
                createdAnt.transform.SetParent(antsParentTransform);

                currentAnts.Add(createdAnt);
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Get the X position an ant should spawn.
        /// </summary>
        private int GetSpawnXPosition()
        {
            int maxX = Blocks.GetLength(0) - 1;
            int minX = 1;

            return RNG.Next(minX, maxX);
        }

        /// <summary>
        /// Get the Z position an ant should spawn.
        /// </summary>
        private int GetSpawnZPosition()
        {
            int maxZ = Blocks.GetLength(2) - 1;
            int minZ = 1;

            return RNG.Next(minZ, maxZ);
        }

        /// <summary>
        /// Get the Y position an ant should spawn, based on the X and Z coordinates.
        /// </summary>
        public float GetSpawnYPosition(int xPosition, int zPosition)
        {
            int maxY = Blocks.GetLength(1) - 1;

            for (int currentY = maxY; currentY >= 0; currentY--)
            {
                AbstractBlock currentBlock = GetBlock(xPosition, currentY, zPosition);
                bool isAir = currentBlock is AirBlock;
                
                if(!isAir)
                {
                    float yPosition = (float) currentY + 1.0f;
                    return yPosition;
                }
            }

            return -1;
        }

        /// <summary>
        /// Used to check if the spawning spot already has an ant.
        /// </summary>
        private bool HasAntAtPosition(int xPosition, int zPosition)
        {
            foreach (GameObject ant in currentAnts)
            {
                bool zOccupied = ant.transform.position.x == (float) xPosition;
                bool xOccupied = ant.transform.position.z == (float) zPosition;
                bool positionOccupied = xOccupied && zOccupied;

                if (positionOccupied)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Retrieves an abstract block type at the desired world coordinates.
        /// </summary>
        public AbstractBlock GetBlock(int WorldXCoordinate, int WorldYCoordinate, int WorldZCoordinate)
        {
            if
            (
                WorldXCoordinate < 0 ||
                WorldYCoordinate < 0 ||
                WorldZCoordinate < 0 ||
                WorldXCoordinate >= Blocks.GetLength(0) ||
                WorldYCoordinate >= Blocks.GetLength(1) ||
                WorldZCoordinate >= Blocks.GetLength(2)
            )
                return new AirBlock();

            return Blocks[WorldXCoordinate, WorldYCoordinate, WorldZCoordinate];
        }

        /// <summary>
        /// Retrieves an abstract block type at the desired local coordinates within a chunk.
        /// </summary>
        public AbstractBlock GetBlock(
            int ChunkXCoordinate, int ChunkYCoordinate, int ChunkZCoordinate,
            int LocalXCoordinate, int LocalYCoordinate, int LocalZCoordinate)
        {
            if
            (
                LocalXCoordinate < 0 ||
                LocalYCoordinate < 0 ||
                LocalZCoordinate < 0 ||
                LocalXCoordinate >= Blocks.GetLength(0) ||
                LocalYCoordinate >= Blocks.GetLength(1) ||
                LocalZCoordinate >= Blocks.GetLength(2) ||
                ChunkXCoordinate < 0 ||
                ChunkYCoordinate < 0 ||
                ChunkZCoordinate < 0 ||
                ChunkXCoordinate >= Blocks.GetLength(0) ||
                ChunkYCoordinate >= Blocks.GetLength(1) ||
                ChunkZCoordinate >= Blocks.GetLength(2) 
            )
                return new AirBlock();

            return Blocks
            [
                ChunkXCoordinate * LocalXCoordinate,
                ChunkYCoordinate * LocalYCoordinate,
                ChunkZCoordinate * LocalZCoordinate
            ];
        }

        /// <summary>
        /// sets an abstract block type at the desired world coordinates.
        /// </summary>
        public void SetBlock(int WorldXCoordinate, int WorldYCoordinate, int WorldZCoordinate, AbstractBlock toSet)
        {
            if
            (
                WorldXCoordinate < 0 ||
                WorldYCoordinate < 0 ||
                WorldZCoordinate < 0 ||
                WorldXCoordinate > Blocks.GetLength(0) ||
                WorldYCoordinate > Blocks.GetLength(1) ||
                WorldZCoordinate > Blocks.GetLength(2)
            )
            {
                Debug.Log("Attempted to set a block which didn't exist");
                return;
            }

            Blocks[WorldXCoordinate, WorldYCoordinate, WorldZCoordinate] = toSet;

            SetChunkContainingBlockToUpdate
            (
                WorldXCoordinate,
                WorldYCoordinate,
                WorldZCoordinate
            );
        }

        /// <summary>
        /// sets an abstract block type at the desired local coordinates within a chunk.
        /// </summary>
        public void SetBlock(
            int ChunkXCoordinate, int ChunkYCoordinate, int ChunkZCoordinate,
            int LocalXCoordinate, int LocalYCoordinate, int LocalZCoordinate,
            AbstractBlock toSet)
        {
            if
            (
                LocalXCoordinate < 0 ||
                LocalYCoordinate < 0 ||
                LocalZCoordinate < 0 ||
                LocalXCoordinate > Blocks.GetLength(0) ||
                LocalYCoordinate > Blocks.GetLength(1) ||
                LocalZCoordinate > Blocks.GetLength(2) ||
                ChunkXCoordinate < 0 ||
                ChunkYCoordinate < 0 ||
                ChunkZCoordinate < 0 ||
                ChunkXCoordinate > Blocks.GetLength(0) ||
                ChunkYCoordinate > Blocks.GetLength(1) ||
                ChunkZCoordinate > Blocks.GetLength(2)
            )
            {
                Debug.Log("Attempted to set a block which didn't exist");
                return;
            }
            Blocks
            [
                ChunkXCoordinate * LocalXCoordinate,
                ChunkYCoordinate * LocalYCoordinate,
                ChunkZCoordinate * LocalZCoordinate
            ] = toSet;

            SetChunkContainingBlockToUpdate
            (
                ChunkXCoordinate * LocalXCoordinate,
                ChunkYCoordinate * LocalYCoordinate,
                ChunkZCoordinate * LocalZCoordinate
            );
        }

        #endregion

        #region Helpers

        #region Blocks

        /// <summary>
        /// Is responsible for generating the base, acid, and spheres.
        /// </summary>
        private void GenerateData()
        {
            GeneratePreliminaryWorld();
            GenerateAcidicRegions();
            GenerateSphericalContainers();
        }

        /// <summary>
        /// Generates the preliminary world data based on perlin noise.
        /// </summary>
        private void GeneratePreliminaryWorld()
        {
            for (int x = 0; x < Blocks.GetLength(0); x++)
                for (int z = 0; z < Blocks.GetLength(2); z++)
                {
                    /**
                     * These numbers have been fine-tuned and tweaked through trial and error.
                     * Altering these numbers may produce weird looking worlds.
                     **/
                    int stoneCeiling = SimplexNoise.GetPerlinNoise(x, 0, z, 10, 3, 1.2) +
                                       SimplexNoise.GetPerlinNoise(x, 300, z, 20, 4, 0) +
                                       10;
                    int grassHeight = SimplexNoise.GetPerlinNoise(x, 100, z, 30, 10, 0);
                    int foodHeight = SimplexNoise.GetPerlinNoise(x, 200, z, 20, 5, 1.5);

                    for (int y = 0; y < Blocks.GetLength(1); y++)
                    {
                        if (y <= stoneCeiling)
                        {
                            Blocks[x, y, z] = new StoneBlock();
                        }
                        else if (y <= stoneCeiling + grassHeight)
                        {
                            Blocks[x, y, z] = new GrassBlock();
                        }
                        else if (y <= stoneCeiling + grassHeight + foodHeight)
                        {
                            Blocks[x, y, z] = new MulchBlock();
                        }
                        else
                        {
                            Blocks[x, y, z] = new AirBlock();
                        }
                        if
                        (
                            x == 0 ||
                            x >= Blocks.GetLength(0) - 1 ||
                            z == 0 ||
                            z >= Blocks.GetLength(2) - 1 ||
                            y == 0
                        )
                            Blocks[x, y, z] = new ContainerBlock();
                    }
                }
        }

        /// <summary>
        /// Alters a pre-generated map so that acid blocks exist.
        /// </summary>
        private void GenerateAcidicRegions()
        {
            for (int i = 0; i < ConfigurationManager.Instance.Number_Of_Acidic_Regions; i++)
            {
                int xCoord = RNG.Next(0, Blocks.GetLength(0));
                int zCoord = RNG.Next(0, Blocks.GetLength(2));
                int yCoord = -1;
                for (int j = Blocks.GetLength(1) - 1; j >= 0; j--)
                {
                    if (Blocks[xCoord, j, zCoord] as AirBlock == null)
                    {
                        yCoord = j;
                        break;
                    }
                }

                //Generate a sphere around this point overriding non-air blocks
                for (int HX = xCoord - ConfigurationManager.Instance.Acidic_Region_Radius; HX < xCoord + ConfigurationManager.Instance.Acidic_Region_Radius; HX++)
                {
                    for (int HZ = zCoord - ConfigurationManager.Instance.Acidic_Region_Radius; HZ < zCoord + ConfigurationManager.Instance.Acidic_Region_Radius; HZ++)
                    {
                        for (int HY = yCoord - ConfigurationManager.Instance.Acidic_Region_Radius; HY < yCoord + ConfigurationManager.Instance.Acidic_Region_Radius; HY++)
                        {
                            float xSquare = (xCoord - HX) * (xCoord - HX);
                            float ySquare = (yCoord - HY) * (yCoord - HY);
                            float zSquare = (zCoord - HZ) * (zCoord - HZ);
                            float Dist = Mathf.Sqrt(xSquare + ySquare + zSquare);
                            if (Dist <= ConfigurationManager.Instance.Acidic_Region_Radius)
                            {
                                int CX, CY, CZ;
                                CX = Mathf.Clamp(HX, 1, Blocks.GetLength(0) - 2);
                                CZ = Mathf.Clamp(HZ, 1, Blocks.GetLength(2) - 2);
                                CY = Mathf.Clamp(HY, 1, Blocks.GetLength(1) - 2);
                                if (Blocks[CX, CY, CZ] as AirBlock != null)
                                    Blocks[CX, CY, CZ] = new AcidicBlock();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Alters a pre-generated map so that obstructions exist within the map.
        /// </summary>
        private void GenerateSphericalContainers()
        {

            //Generate hazards
            for (int i = 0; i < ConfigurationManager.Instance.Number_Of_Conatiner_Spheres; i++)
            {
                int xCoord = RNG.Next(0, Blocks.GetLength(0));
                int zCoord = RNG.Next(0, Blocks.GetLength(2));
                int yCoord = RNG.Next(0, Blocks.GetLength(1));


                //Generate a sphere around this point overriding non-air blocks
                for (int HX = xCoord - ConfigurationManager.Instance.Conatiner_Sphere_Radius; HX < xCoord + ConfigurationManager.Instance.Conatiner_Sphere_Radius; HX++)
                {
                    for (int HZ = zCoord - ConfigurationManager.Instance.Conatiner_Sphere_Radius; HZ < zCoord + ConfigurationManager.Instance.Conatiner_Sphere_Radius; HZ++)
                    {
                        for (int HY = yCoord - ConfigurationManager.Instance.Conatiner_Sphere_Radius; HY < yCoord + ConfigurationManager.Instance.Conatiner_Sphere_Radius; HY++)
                        {
                            float xSquare = (xCoord - HX) * (xCoord - HX);
                            float ySquare = (yCoord - HY) * (yCoord - HY);
                            float zSquare = (zCoord - HZ) * (zCoord - HZ);
                            float Dist = Mathf.Sqrt(xSquare + ySquare + zSquare);
                            if (Dist <= ConfigurationManager.Instance.Conatiner_Sphere_Radius)
                            {
                                int CX, CY, CZ;
                                CX = Mathf.Clamp(HX, 1, Blocks.GetLength(0) - 2);
                                CZ = Mathf.Clamp(HZ, 1, Blocks.GetLength(2) - 2);
                                CY = Mathf.Clamp(HY, 1, Blocks.GetLength(1) - 2);
                                Blocks[CX, CY, CZ] = new ContainerBlock();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Given a world coordinate, tells the chunk holding that coordinate to update.
        /// Also tells all 4 neighbours to update (as an altered block might exist on the
        /// edge of a chunk).
        /// </summary>
        /// <param name="worldXCoordinate"></param>
        /// <param name="worldYCoordinate"></param>
        /// <param name="worldZCoordinate"></param>
        private void SetChunkContainingBlockToUpdate(int worldXCoordinate, int worldYCoordinate, int worldZCoordinate)
        {
            //Updates the chunk containing this block
            int updateX = Mathf.FloorToInt(worldXCoordinate / ConfigurationManager.Instance.Chunk_Diameter);
            int updateY = Mathf.FloorToInt(worldYCoordinate / ConfigurationManager.Instance.Chunk_Diameter);
            int updateZ = Mathf.FloorToInt(worldZCoordinate / ConfigurationManager.Instance.Chunk_Diameter);
            Chunks[updateX, updateY, updateZ].updateNeeded = true;
            
            // Also flag all 6 neighbours for update as well
            if(updateX - 1 >= 0)
                Chunks[updateX - 1, updateY, updateZ].updateNeeded = true;
            if (updateX + 1 < Chunks.GetLength(0))
                Chunks[updateX + 1, updateY, updateZ].updateNeeded = true;

            if (updateY - 1 >= 0)
                Chunks[updateX, updateY - 1, updateZ].updateNeeded = true;
            if (updateY + 1 < Chunks.GetLength(1))
                Chunks[updateX, updateY + 1, updateZ].updateNeeded = true;

            if (updateZ - 1 >= 0)
                Chunks[updateX, updateY, updateZ - 1].updateNeeded = true;
            if (updateX + 1 < Chunks.GetLength(2))
                Chunks[updateX, updateY, updateZ + 1].updateNeeded = true;
        }

        #endregion

        #region Chunks

        /// <summary>
        /// Takes the world data and generates the associated chunk objects.
        /// </summary>
        private void GenerateChunks()
        {
            GameObject chunkObg = new GameObject("Chunks");

            for (int x = 0; x < Chunks.GetLength(0); x++)
                for (int z = 0; z < Chunks.GetLength(2); z++)
                    for (int y = 0; y < Chunks.GetLength(1); y++)
                    {
                        GameObject temp = new GameObject();
                        temp.transform.parent = chunkObg.transform;
                        temp.transform.position = new Vector3
                        (
                            x * ConfigurationManager.Instance.Chunk_Diameter - 0.5f,
                            y * ConfigurationManager.Instance.Chunk_Diameter + 0.5f,
                            z * ConfigurationManager.Instance.Chunk_Diameter - 0.5f
                        );
                        Chunk chunkScript = temp.AddComponent<Chunk>();
                        chunkScript.x = x * ConfigurationManager.Instance.Chunk_Diameter;
                        chunkScript.y = y * ConfigurationManager.Instance.Chunk_Diameter;
                        chunkScript.z = z * ConfigurationManager.Instance.Chunk_Diameter;
                        chunkScript.Init(blockMaterial);
                        chunkScript.GenerateMesh();
                        Chunks[x, y, z] = chunkScript;
                    }
        }

        #endregion

        #endregion
    }
}
