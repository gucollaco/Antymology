using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Antymology.Terrain;

public class Queen : MonoBehaviour
{
    public int health;
    public int maxHealth = 500;
    public int totalGivenHealth = 0;
    public int totalReceivedHealth = 0;

    private int maxHealthOffset = 20;
    private int mulchHealthRecovery = 30;
    private int maxDamage = 1;
    private System.Random RNG;
    private List<MoveDirections> possibleDirections;
    private List<float> possibleDirectionsHeightUpdate;
    private int turn = 0;
    
    // Start is called before the first frame update
    void Start()
    {
        // Generate new random number generator
        RNG = new System.Random(ConfigurationManager.Instance.Seed);
        
        // Randomize initial health
        health = maxHealth - Random.Range(0, maxHealthOffset + 1);

        possibleDirections = new List<MoveDirections>();
        possibleDirectionsHeightUpdate = new List<float>();
    }

    // Update is called once per frame
    void Update()
    {
    }

    /// <summary>
    /// Method to choose the ant's next action.
    /// </summary>
    public void ChooseAction()
    {
        // ClearDirectionArrays();

        // Vector3 belowBlockPosition = GetBelowBlockPosition();
        // AbstractBlock belowBlock = WorldManager.Instance.GetBlock((int) belowBlockPosition.x, (int) belowBlockPosition.y, (int) belowBlockPosition.z);
        // BlockType belowBlockType = GetBelowBlockType(belowBlock);

        // // Get the ant with the lowest health compared to the total.
        // GameObject lowestHealthObject = null;
        // float lowestHealth = 10000;
        
        // foreach (GameObject antObject in WorldManager.Instance.currentAnts)
        // {
        //     Ant ant = antObject.GetComponent<Ant>();

        //     float currentAntHealth = ant.health / ant.maxHealth;
        //     if (currentAntHealth < lowestHealth)
        //     {
        //         lowestHealthObject = antObject;
        //         lowestHealth = currentAntHealth;
        //     }
        // }

        // MoveToTarget(lowestHealthObject);
        UpdateHealth();

        turn++;

        // float moveLeftHeight = GetMoveHeight((int) transform.position.x - 1, (int) transform.position.y);
        // bool canMoveLeft = moveLeftHeight <= 1;

        // float moveRightHeight = GetMoveHeight((int) transform.position.x + 1, (int) transform.position.y);
        // bool canMoveRight = moveRightHeight <= 1;
    
        // float moveBackwardHeight = GetMoveHeight((int) transform.position.x, (int) transform.position.y - 1);
        // bool canMoveBackward = moveBackwardHeight <= 1;
    
        // float moveForwardHeight = GetMoveHeight((int) transform.position.x, (int) transform.position.y + 1);
        // bool canMoveForward = moveForwardHeight <= 1;

        // if(canMoveLeft)
        // {
        //     possibleDirections.Add(MoveDirections.Left);
        //     possibleDirectionsHeightUpdate.Add(moveLeftHeight);
        // }
        // if(canMoveRight)
        // {
        //     possibleDirections.Add(MoveDirections.Right);
        //     possibleDirectionsHeightUpdate.Add(moveRightHeight);
        // }
        // if(canMoveBackward)
        // {
        //     possibleDirections.Add(MoveDirections.Backward);
        //     possibleDirectionsHeightUpdate.Add(moveBackwardHeight);
        // }
        // if(canMoveForward)
        // {
        //     possibleDirections.Add(MoveDirections.Forward);
        //     possibleDirectionsHeightUpdate.Add(moveForwardHeight);
        // }

        // int chosenDirectionIndex = RNG.Next(possibleDirections.Count);
        
        // MoveTo(chosenDirectionIndex);
    }

    /// <summary>
    /// Method to update the ant's health per turn.
    /// </summary>
    public void UpdateHealth()
    {
        health -= maxDamage;
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

        float currentHeight = GetPositionHeight(transform.position.x, transform.position.z);
        if (xDistance > zDistance)
        {
            float targetHeight = GetPositionHeight(transform.position.x + xUpdate, transform.position.z);
            float heightDifference = targetHeight - currentHeight;

            if (heightDifference == 1.0f)
                transform.position = transform.position + new Vector3(xUpdate, 1.0f, 0);
            else if (heightDifference == -1.0f)
                transform.position = transform.position + new Vector3(xUpdate, -1.0f, 0);
            else if (heightDifference == 0.0f)
                transform.position = transform.position + new Vector3(xUpdate, 0, 0);
            else if (heightDifference >= 2.0f)
            {
                // Should place a stone block on the current position.
                AbstractBlock block = new StoneBlock();
                Vector3 oldPosition = transform.position;
                transform.position = transform.position + new Vector3(0, 1.0f, 0);
                PlaceBlock(oldPosition.x, oldPosition.y, oldPosition.z, block);
            }
            else if (heightDifference <= -2.0f)
            {
                // Should remove the below block, by replacing it with an air block.
                AbstractBlock block = new AirBlock();
                PlaceBlock(transform.position.x, transform.position.y - 1.0f, transform.position.z, block);
                transform.position = transform.position + new Vector3(0, -1.0f, 0);
            }
        }
        else
        {
            float targetHeight = GetPositionHeight(transform.position.x, transform.position.z + zUpdate);
            float heightDifference = targetHeight - currentHeight;

            if (heightDifference == 1.0f)
                transform.position = transform.position + new Vector3(0, 1.0f, zUpdate);
            else if (heightDifference == -1.0f)
                transform.position = transform.position + new Vector3(0, -1.0f, zUpdate);
            else if (heightDifference == 0.0f)
                transform.position = transform.position + new Vector3(0, 0, zUpdate);
            else if (heightDifference >= 2.0f)
            {
                // Should place a stone block on the current position.
                AbstractBlock block = new StoneBlock();
                Vector3 oldPosition = transform.position;
                transform.position = transform.position + new Vector3(0, 1.0f, 0);
                PlaceBlock(oldPosition.x, oldPosition.y, oldPosition.z, block);
            }
            else if (heightDifference <= -2.0f)
            {
                // Should remove the below block, by replacing it with an air block.
                AbstractBlock block = new AirBlock();
                PlaceBlock(transform.position.x, transform.position.y - 1.0f, transform.position.z, block);
                transform.position = transform.position + new Vector3(0, -1.0f, 0);
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
    /// Function to simulate a dig.
    /// </summary>
    private void Dig(int x, int y, int z, AbstractBlock block)
    {
        // WorldManager.Instance.SetBlock(x, y, z, block);
    }

    /// <summary>
    /// Function to place a block at a given coordinate.
    /// </summary>
    private void PlaceBlock(float x, float y, float z, AbstractBlock block)
    {
        WorldManager.Instance.SetBlock((int) x, (int) y, (int) z, block);
    }

    /// <summary>
    /// Clear the move direction related arrays.
    /// </summary>
    public void ClearDirectionArrays()
    {
        possibleDirections.Clear();
        possibleDirectionsHeightUpdate.Clear();
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
    /// Method to move to a direction.
    /// </summary>
    public void MoveTo(int chosenDirectionIndex)
    {
        MoveDirections chosenDirection = possibleDirections[chosenDirectionIndex];
        float yPositionUpdate = possibleDirectionsHeightUpdate[chosenDirectionIndex];

        if (chosenDirection == MoveDirections.Left)
            transform.position = GetPosition() + new Vector3(-1, yPositionUpdate, 0);
        else if (chosenDirection == MoveDirections.Right)
            transform.position = GetPosition() + new Vector3(1, yPositionUpdate, 0);
        else if (chosenDirection == MoveDirections.Backward)
            transform.position = GetPosition() + new Vector3(0, yPositionUpdate, -1);
        else if (chosenDirection == MoveDirections.Forward)
            transform.position = GetPosition() + new Vector3(0, yPositionUpdate, 1);
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
    /// Method to get the block's type.
    /// </summary>
    private BlockType GetBelowBlockType(AbstractBlock block)
    {
        if (block is AcidicBlock)
            return BlockType.Acid;
        else if (block is AirBlock)
            return BlockType.Air;
        else if (block is ContainerBlock)
            return BlockType.Container;
        else if (block is GrassBlock)
            return BlockType.Mulch;
        else if (block is NestBlock)
            return BlockType.Nest;
        else if (block is StoneBlock)
            return BlockType.Stone;
        
        return BlockType.Abstract;
    }
}
