using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Antymology.Terrain;

public class Queen : MonoBehaviour
{
    public int health;
    public int initialHealth = 500;
    public int maxHealth = 700;
    public int totalGivenHealth = 0;
    public int totalReceivedHealth = 0;
    public int totalMulchRecoveredHealth = 0;
    public int awaitTurns = 0;
    public bool anotherOnSamePosition = false;
    public int nestsProduced = 0;
    public int daysForNestProduction = 75;

    private int mulchHealthRecovery = 40;
    private int turnDamage = 6;
    private System.Random RNG;
    
    /// <summary>
    /// Awake method.
    /// </summary>
    void Awake()
    {
        // Setting initial health
        health = initialHealth;
    }

    /// <summary>
    /// Start is called before the first frame update
    /// </summary>
    void Start()
    {
        // Generate new random number generator
        RNG = new System.Random(ConfigurationManager.Instance.Seed);
    }

    /// <summary>
    /// Called every 1s (configured at Edit > Settings > Time > Fixed Timestep).
    /// </summary>
    private void FixedUpdate()
    {
        GameObject lowestHealthAnt = GetAntWithLowestHealth(WorldManager.Instance.currentAnts.ToArray());
        ChooseAction(lowestHealthAnt);
    }

    /// <summary>
    /// Method to choose the queen's next action.
    /// </summary>
    public void ChooseAction(GameObject lowestHealthAnt)
    {
        BlockType belowBlockType = GetBelowBlockType();

        Action(belowBlockType, lowestHealthAnt);
        UpdateHealth(belowBlockType);
        UpdateAwaitTurns();
    }

    /// <summary>
    /// Will decide which action the queen should take.
    /// </summary>
    private void Action(BlockType belowBlockType, GameObject lowestHealthAnt)
    {
        bool hasNestProductionDaysPassed = WorldManager.Instance.turn % daysForNestProduction == 0;

        // If the expected amount of turns have passed, the queen produces a nest.
        if (hasNestProductionDaysPassed)
            ProduceNest();
        // If haven't shared health recently, will move to the target.
        else if (awaitTurns == 0)
        {
            DecideMove(belowBlockType, lowestHealthAnt);
            CheckPosition();
        }
        // If not, will move randomly.
        else
            MoveRandomly();
    }

    /// <summary>
    /// To a random direction.
    /// </summary>
    private void ProduceNest()
    {
        // Should place a nest block on the current position.
        AbstractBlock block = new NestBlock();
        Vector3 oldPosition = transform.position;
        transform.position = transform.position + new Vector3(0, 1.0f, 0);
        PlaceBlock(oldPosition.x, oldPosition.y, oldPosition.z, block);

        // Loses one third of the current health.
        float oneThirdOfHealth = health / 3;
        health -= (int) Mathf.Round(oneThirdOfHealth);

        // Increases the quantity of produced nests.
        nestsProduced++;
    }

    /// <summary>
    /// To a random direction.
    /// </summary>
    private void MoveRandomly()
    {
        BlockType belowBlockType = GetBelowBlockType();
        DecideRandomMove(belowBlockType);
    }

    /// <summary>
    /// Decide which random move to take.
    /// </summary>
    private void DecideRandomMove(BlockType belowBlockType)
    {
        // Decide randomly which axis to travel through.
        float randomAxis = Random.value;

        // Decide randomly which direction (forward or backward) to go to in that axis.
        float randomDirection = Random.value;
        int directionUpdate = randomDirection > 0.5f ? 1 : -1;

        if (randomAxis < 0.5f)
            MoveTowardsZ(directionUpdate);
        else
            MoveTowardsX(directionUpdate);
    }

    /// <summary>
    /// Decide which move to take.
    /// </summary>
    private void DecideMove(BlockType belowBlockType, GameObject lowestHealthObject)
    {
        // If below block is of type Mulch, and the current health is less than (maxHealth - mulchHealthRecovery)
        // we have a 20% chance that the ant will dig and feed from it.
        if (belowBlockType == BlockType.Mulch && health < (maxHealth - mulchHealthRecovery))
        {
            float rand = Random.value;
            if (rand < 0.2f)
                Dig();
            else
                MoveToTarget(lowestHealthObject);
        }
        else
            MoveToTarget(lowestHealthObject);
    }

    /// <summary>
    /// * Considering the queen to also be an ant.
    /// Checks if there are other ants on this position. If so, the ant with the highest health will donate health to the one with less health.
    /// This action makes the ant get into an await state, having to wait for an amount of turns to be able to interact with other ants.
    /// The ant will move randomly on this state.
    /// </summary>
    private void CheckPosition()
    {
        anotherOnSamePosition = false;

        // has other gameobject on same position
        foreach (GameObject ant in WorldManager.Instance.currentAnts)
        {
            // If on the same position
            if (ant.transform.position == gameObject.transform.position && ant.name != gameObject.name)
            {
                anotherOnSamePosition = true;
                Ant otherAntScript = ant.GetComponent<Ant>();
                
                // Share health with other ant in case the other ant has less remaining health.
                if (otherAntScript.health < health)
                {
                    int randomHealthDonation = Random.Range(50, 100);
                    otherAntScript.health += randomHealthDonation;
                    otherAntScript.totalReceivedHealth += randomHealthDonation;
                    totalGivenHealth += randomHealthDonation;
                    health -= randomHealthDonation;

                    // Will move randomly and not be able to give/receive health for some turns.
                    awaitTurns = 20;
                }
            }
        }
    }

    /// <summary>
    /// Method to update the ant's health per turn.
    /// </summary>
    public void UpdateHealth(BlockType belowBlockType)
    {
        health -= turnDamage;

        if (belowBlockType == BlockType.Acid)
            health -= turnDamage;

        if (health <= 0)
            gameObject.SetActive(false);
    }

    /// <summary>
    /// Method to update await turns value, in case needed.
    /// </summary>
    public void UpdateAwaitTurns()
    {
        if (awaitTurns > 0)
            awaitTurns--;
    }

    /// <summary>
    /// Method to move the ant to the target.
    /// </summary>
    public void MoveToTarget(GameObject targetObject)
    {
        float xDistance = targetObject.gameObject.transform.position.x - gameObject.transform.position.x;
        float xUpdate = xDistance >= 0 ? 1 : -1;
        float zDistance = targetObject.gameObject.transform.position.z - gameObject.transform.position.z;
        float zUpdate = zDistance >= 0 ? 1 : -1;

        if (xDistance == 0)
            MoveTowardsZ(zUpdate);
        else if (zDistance == 0)
            MoveTowardsX(xUpdate);
        else
        {
            // randomize if should go through X or Z on this step
            float rand = Random.value;
            if (rand < 0.5f)
                MoveTowardsX(xUpdate);
            else
                MoveTowardsZ(zUpdate);
        }
    }

    /// <summary>
    /// Method to move towards the target through X.
    /// </summary>
    public void MoveTowardsX(float xUpdate)
    {
        float currentHeight = GetPositionHeight(transform.position.x, transform.position.z);
        float targetHeight = GetPositionHeight(transform.position.x + xUpdate, transform.position.z);
        float heightDifference = targetHeight - currentHeight;

        if (heightDifference == 1.0f)
            transform.position = transform.position + new Vector3(xUpdate, 1.0f, 0);
        else if (heightDifference == -1.0f)
            transform.position = transform.position + new Vector3(xUpdate, -1.0f, 0);
        else if (heightDifference == 0.0f)
            transform.position = transform.position + new Vector3(xUpdate, 0, 0);
        else if (heightDifference >= 2.0f)
            Climb();
        else if (heightDifference <= -2.0f)
            Dig();
    }

    /// <summary>
    /// Method to move towards the target through Z.
    /// </summary>
    public void MoveTowardsZ(float zUpdate)
    {
        float currentHeight = GetPositionHeight(transform.position.x, transform.position.z);
        float targetHeight = GetPositionHeight(transform.position.x, transform.position.z + zUpdate);
        float heightDifference = targetHeight - currentHeight;

        if (heightDifference == 1.0f)
            transform.position = transform.position + new Vector3(0, 1.0f, zUpdate);
        else if (heightDifference == -1.0f)
            transform.position = transform.position + new Vector3(0, -1.0f, zUpdate);
        else if (heightDifference == 0.0f)
            transform.position = transform.position + new Vector3(0, 0, zUpdate);
        else if (heightDifference >= 2.0f)
            Climb();
        else if (heightDifference <= -2.0f)
            Dig();
    }

    /// <summary>
    /// Climbing a placed block.
    /// </summary>
    public void Climb()
    {
        // Should place a stone block on the current position.
        AbstractBlock block = new StoneBlock();
        Vector3 oldPosition = transform.position;
        transform.position = transform.position + new Vector3(0, 1.0f, 0);
        PlaceBlock(oldPosition.x, oldPosition.y, oldPosition.z, block);
    }
    
    /// <summary>
    /// Digging on the current position.
    /// </summary>
    public void Dig()
    {
        BlockType belowBlockType = GetBelowBlockType();

        // Will dig when below block is not container, and when there are no other ants on the same position.
        if (belowBlockType != BlockType.Container && !anotherOnSamePosition)
        {
            // Should remove the below block, by replacing it with an air block.
            AbstractBlock block = new AirBlock();
            PlaceBlock(transform.position.x, transform.position.y - 1.0f, transform.position.z, block);
            transform.position = transform.position + new Vector3(0, -1.0f, 0);

            // Recovers health when digging mulch.
            if (belowBlockType == BlockType.Mulch)
            {
                health += mulchHealthRecovery;
                totalMulchRecoveredHealth += mulchHealthRecovery;
                // There is still a chance of eating when not needed.
                if (health > maxHealth)
                {
                    health = maxHealth;
                    totalMulchRecoveredHealth -= (health - maxHealth);
                }
            }
        }
    }

    /// <summary>
    /// Getting the height when moving to another position.
    /// </summary>
    public float GetPositionHeight(float xPosition, float zPosition)
    {
        float yPosition = WorldManager.Instance.GetSpawnYPosition((int) xPosition, (int) zPosition);
        // Disconsidering the ant's square.
        return yPosition - 1.0f;
    }
    
    /// <summary>
    /// Function to place a block at a given coordinate.
    /// </summary>
    private void PlaceBlock(float x, float y, float z, AbstractBlock block)
    {
        WorldManager.Instance.SetBlock((int) x, (int) y, (int) z, block);
    }

    /// <summary>
    /// Getting the height when moving to another position.
    /// </summary>
    public float GetMoveHeight(int xPosition, int zPosition)
    {
        float yPosition = WorldManager.Instance.GetSpawnYPosition((int) transform.position.x + 1, (int) transform.position.z);

        return Mathf.Abs(transform.position.y - yPosition);
    }
    
    /// <summary>
    /// Method to get the ant's current position.
    /// </summary>
    private Vector3 GetPosition()
    {
        return transform.position;
    }

    /// <summary>
    /// Method to get the block that's below the ant's current position.
    /// </summary>
    private Vector3 GetBelowBlockPosition()
    {
        return GetPosition() + new Vector3(0, -1.0f, 0);
    }

    /// <summary>
    /// Method to get the below block's type.
    /// </summary>
    private BlockType GetBelowBlockType()
    {
        Vector3 belowBlockPosition = GetBelowBlockPosition();
        AbstractBlock belowBlock = WorldManager.Instance.GetBlock((int) belowBlockPosition.x, (int) belowBlockPosition.y, (int) belowBlockPosition.z);
        BlockType belowBlockType = GetBlockType(belowBlock);
        return belowBlockType;
    }

    /// <summary>
    /// Method to get the block's type.
    /// </summary>
    private BlockType GetBlockType(AbstractBlock block)
    {
        if (block is AcidicBlock)
            return BlockType.Acid;
        else if (block is AirBlock)
            return BlockType.Air;
        else if (block is ContainerBlock)
            return BlockType.Container;
        else if (block is GrassBlock)
            return BlockType.Grass;
        else if (block is MulchBlock)
            return BlockType.Mulch;
        else if (block is NestBlock)
            return BlockType.Nest;
        else if (block is StoneBlock)
            return BlockType.Stone;
        
        return BlockType.Abstract;
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
        
        // Debug.Log("lowestHealthObject");
        // Debug.Log(lowestHealthObject.name);

        return lowestHealthObject;
    }
}
