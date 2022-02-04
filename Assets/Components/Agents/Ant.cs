using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Antymology.Terrain;

public enum BlockType
{
    Abstract = 0,
    Acid = 1,
    Air = 2,
    Container = 3,
    Grass = 4,
    Mulch = 5,
    Nest = 6,
    Stone = 7,
}

public enum MoveDirections
{
    Forward = 0,
    Right = 1,
    Backward = 2,
    Left = 3,
}

public class Ant : MonoBehaviour
{
    public int health;
    public int initialHealth = 250;
    public int maxHealth = 350;
    public int totalGivenHealth = 0;
    public int totalReceivedHealth = 0;
    public int totalMulchRecoveredHealth = 0;
    public int awaitTurns = 0;
    public bool anotherOnSamePosition = false;

    private int initialHealthOffset = 20;
    private int mulchHealthRecovery = 40;
    private int turnDamage = 6;
    private System.Random RNG;
    
    /// <summary>
    /// Start is called before the first frame update
    /// </summary>
    void Start()
    {
        // Generate new random number generator
        RNG = new System.Random(ConfigurationManager.Instance.Seed);
        
        // Randomize initial health
        health = initialHealth - Random.Range(0, initialHealthOffset + 1);
    }

    /// <summary>
    /// Method to get the element with the lowest remaining health.
    /// </summary>
    public GameObject GetElementWithLowestHealth(GameObject lowestHealthAnt)
    {
        bool isQueenTheLowest = false;

        // Get the queen's health.
        Queen queenScript = WorldManager.Instance.currentQueen.GetComponent<Queen>();
        Ant antScript = lowestHealthAnt.GetComponent<Ant>();

        // Check if the queen is the ant with the lowest remaining health.
        isQueenTheLowest = queenScript.health < antScript.maxHealth;

        // If this element is the one with lowest health, go to the queen.
        // Or, if the queen is the element with the lowest health
        if (gameObject.name == lowestHealthAnt.gameObject.name || isQueenTheLowest)
            return WorldManager.Instance.currentQueen;
        else
            return lowestHealthAnt;
    }
    
    /// <summary>
    /// Method to choose the ant's next action.
    /// </summary>
    public void ChooseAction(GameObject lowestHealthAnt)
    {
        BlockType belowBlockType = GetBelowBlockType();

        Action(belowBlockType, lowestHealthAnt);
        UpdateHealth(belowBlockType);
        UpdateAwaitTurns();
    }

    /// <summary>
    /// Will decide which action the ant should take.
    /// </summary>
    private void Action(BlockType belowBlockType, GameObject lowestHealthAnt)
    {
        // If haven't shared health recently, will move to the target.
        if (awaitTurns == 0)
        {
            GameObject lowestHealthObject = GetElementWithLowestHealth(lowestHealthAnt);
            DecideMove(belowBlockType, lowestHealthObject);
            CheckPosition();
        }
        // If not, will move randomly.
        else
            MoveRandomly();
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
        if (anotherOnSamePosition)
            MoveToTarget(lowestHealthObject);
        // If below block is of type Mulch, and the current health is less than (maxHealth - mulchHealthRecovery)
        // we have a some chance that the ant will dig and feed from it.
        else if (belowBlockType == BlockType.Mulch && health < (maxHealth - mulchHealthRecovery))
        {
            float rand = Random.value;
            if (rand < 0.4f)
                Dig();
            else
                MoveToTarget(lowestHealthObject);
        }
        else
            MoveToTarget(lowestHealthObject);
    }

    /// <summary>
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
                    // Debug.Log("Sharing health with an Ant");
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

        // Checks if the queen is on the same position as the ant.
        if (WorldManager.Instance.currentQueen.transform.position == gameObject.transform.position)
        {
            anotherOnSamePosition = true;
            Queen queenScript = WorldManager.Instance.currentQueen.GetComponent<Queen>();
            
            // Share health with other ant in case the other ant has less remaining health.
            if (queenScript.health < health)
            {
                // Debug.Log("Sharing health with the Queen");
                int randomHealthDonation = Random.Range(50, 100);
                queenScript.health += randomHealthDonation;
                queenScript.totalReceivedHealth += randomHealthDonation;
                totalGivenHealth += randomHealthDonation;
                health -= randomHealthDonation;

                // Will move randomly and not be able to give/receive health for some turns.
                awaitTurns = 20;
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
        else if (heightDifference >= 2.0f && !anotherOnSamePosition)
            Climb();
        else if (heightDifference <= -2.0f && !anotherOnSamePosition)
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
        else if (heightDifference >= 2.0f && !anotherOnSamePosition)
            Climb();
        else if (heightDifference <= -2.0f && !anotherOnSamePosition)
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
}
