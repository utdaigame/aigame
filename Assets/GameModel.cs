using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class GameModel : MonoBehaviour
{
    // enum and structure for accessing character actions
    public enum CharacterAction{MoveNorth, MoveSouth, MoveEast, MoveWest, PickUp, DoNothing};
    Action<Character>[] actions =
        {
            (c)=>{ },
            (c)=>{ },
            (c)=>{ },
            (c)=>{ },
            (c)=>{ },
            (c)=>{ }
        };
    

    private abstract class Entity
    {
        //ID
        uint ID;
        //Name
        string name;
        //Tags
        string[] tags;
    }
    private class Food : Entity
    {
        //Food Value
        double foodValue;
    }
    private class Character : Entity
    {
        //Mind ID
        uint mindID;
        //Stamina
        double stamina;
        //Actions
        CharacterAction[] assignedActions;
    }
    private abstract class AIMind
    {
        //Mind ID
        uint mindID;
        //get next action
        protected abstract CharacterAction getNextAction();
    }
    private class GreedySearchAIMind
    {
        CharacterAction getNextAction()
        {
            // This really need to do something ...
            return CharacterAction.DoNothing;
        }
    }
    private class SpecialAIMind
    {
        //thoughts
        private struct Thought
        {
            //character action
            //next thoughts
            //weights
        }
        //thought priority queue

        //short term memory queue
        Thought[] memory;
        //most recent stamina change
        double staminaChange;

        CharacterAction getNextAction()
        {
            CharacterAction pickedAction = CharacterAction.DoNothing;
            //refill priority queue if needed
            //pop thought from priority queue
            //add next thoughts to priority queue
            //set picked action
            //trim priority queue
            //return picked action
            return pickedAction;
        }
    }

    private int[,] map = new int[4,4];

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // FixedUpdate is called once per game frame
    void FixedUpdate()
    {
        
        // temporary display
        string s = "";
        for (int i = 0; i < map.GetLength(0); i++)
        {
            for (int j = 0; j < map.GetLength(1); j++)
            {
                s += map[i, j] + " ";
            }
            s += "\n";
        }
        Debug.Log(s);
    }
}
