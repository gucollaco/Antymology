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

    private int initialHealthOffset = 20;
    private int mulchHealthRecovery = 30;
    private int turnDamage = 5;
    private System.Random RNG;
    
    void Start()
    {
        // Generate new random number generator
        RNG = new System.Random(ConfigurationManager.Instance.Seed);
        
        // Randomize initial health
        health = initialHealth - Random.Range(0, initialHealthOffset + 1);
    }

    /// <summary>
    /// Method to choose the ant's next action.
    /// </summary>
    public void ChooseAction()
    {
        Vector3 belowBlockPosition = GetBelowBlockPosition();
        AbstractBlock belowBlock = WorldManager.Instance.GetBlock((int) belowBlockPosition.x, (int) belowBlockPosition.y, (int) belowBlockPosition.z);
        BlockType belowBlockType = GetBlockType(belowBlock);

        // Get the element with the lowest health compared to the total.
        GameObject lowestHealthObject = WorldManager.Instance.currentQueen;
        float lowestHealth = lowestHealthObject.GetComponent<Queen>().health / lowestHealthObject.GetComponent<Queen>().maxHealth;
        
        foreach (GameObject antObject in WorldManager.Instance.currentAnts)
        {
            Ant ant = antObject.GetComponent<Ant>();

            float currentAntHealth = ant.health / ant.maxHealth;
            if (currentAntHealth < lowestHealth)
            {
                lowestHealthObject = antObject;
                lowestHealth = currentAntHealth;
            }
        }

        // If the element is the one with lowest health, go to the queen.
        if (gameObject.name == lowestHealthObject.gameObject.name)
        {
            lowestHealthObject = WorldManager.Instance.currentQueen;
            lowestHealth = lowestHealthObject.GetComponent<Queen>().health / lowestHealthObject.GetComponent<Queen>().maxHealth;
        }

        // If below block is of type Mulch, and the current health is less than (maxHealth - mulchHealthRecovery)
        // we have a 60% chance that the ant will dig and feed from it.
        if (belowBlockType == BlockType.Mulch && health < (maxHealth - mulchHealthRecovery))
        {
            float rand = Random.Range(0, 5);
            
            if (rand <= 2)
                Dig();
            else
                MoveToTarget(lowestHealthObject);
        }
        else
            MoveToTarget(lowestHealthObject);

        UpdateHealth(belowBlockType);
    }

    /// <summary>
    /// Detecting collision.
    /// </summary>
    private void OnCollisionEnter(Collision other)
    {
        Debug.Log(gameObject.name);
        Debug.Log("COLLIDED WITH");
        Debug.Log(other.gameObject.name);
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
            float rand = Random.Range(0, 2);

            if (rand == 0)
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
        AbstractBlock block = new MulchBlock();
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
        if (belowBlockType != BlockType.Container)
        {
            // Should remove the below block, by replacing it with an air block.
            AbstractBlock block = new AirBlock();
            PlaceBlock(transform.position.x, transform.position.y - 1.0f, transform.position.z, block);
            transform.position = transform.position + new Vector3(0, -1.0f, 0);
        }
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
