using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class GameModel
{
    private class AIMind1 : AIMind
    {
        //Short Memory Length
        private const int SML = 5;
        
        //random
        System.Random rand = new System.Random();
        //thought list
        List<Thought> allThoughts = new List<Thought>();
        //thought priority queue
        ThoughtQueue thoughtQueue = new ThoughtQueue(10);
        //short term memory queue
        Thought[] thoughtMemory = new Thought[SML];
        //most recent stamina change
        double staminaChange = 0.0;
        double stamina;
        //short term state memory
        (State, Thought)[] stateMemory = new (State, Thought)[SML];


        //thoughts
        private class Thought
        {
            //character action
            public GameModel.CharacterAction? action = null;
            //next thoughts
            public ThoughtQueue nextThoughts = new ThoughtQueue(3);

            public Thought()
            {
                nextThoughts.bump(this, -0.1);
            }
        }
        //thought priority queue class
        private class ThoughtQueue
        {
            public List<Thought> thoughtQueue;
            public List<double> priority;
            private int maxLength;

            public ThoughtQueue(int maxLength)
            {
                thoughtQueue = new List<Thought>();
                priority = new List<double>();
                this.maxLength = maxLength;
            }

            //bump a thought
            public void bump(Thought t, double weight)
            {
                if (!thoughtQueue.Contains(t))
                {
                    //find index of first lower priority thought
                    int count = thoughtQueue.Count;
                    int index = 0;
                    while (index < count)
                    {
                        if (priority[index] <= weight) break;
                        index++;
                    }
                    thoughtQueue.Insert(index, t);
                    priority.Insert(index, weight);

                    //enforce maxLength
                    if (thoughtQueue.Count > maxLength)
                    {
                        thoughtQueue.RemoveAt(maxLength);
                        priority.RemoveAt(maxLength);
                    }
                }
                else // <-- this is inefficient
                {
                    int index = thoughtQueue.IndexOf(t);
                    double newPriority = priority[index] + weight;
                    thoughtQueue.RemoveAt(index);
                    priority.RemoveAt(index);

                    //find index of first lower priority thought
                    int count = thoughtQueue.Count;
                    index = 0;
                    while (index < count)
                    {
                        if (priority[index] <= weight) break;
                        index++;
                    }
                    thoughtQueue.Insert(index, t);
                    priority.Insert(index, newPriority);
                }
            }
            //pop the highest priority/weight thought
            public Thought pop()
            {
                if (thoughtQueue.Count == 0) return null;

                Thought t = thoughtQueue[0];
                thoughtQueue.RemoveAt(0);
                priority.RemoveAt(0);
                return t;
            }
            //get the count of the number of thoughts
            public int count()
            {
                return thoughtQueue.Count;
            }
        }


        private class State
        {
            //state list
            private static List<State> allStates = new List<State>();
            //radial vision
            private const double visionRadius = 1.5;
            //these ints are intended to be like custom enums, their values should be treated as symbolic
            public List<int> stateItems;
            //next thoughts
            public ThoughtQueue nextThoughts = new ThoughtQueue(3);

            public static State getState(Entity entity, Food[,] foodMap)
            {
                List<int> listState = new List<int>();
                for (int x = (int) (-visionRadius); (double) x <= visionRadius; x++)
                {
                    int xsquared = x * x;
                    for (int y = (int) (-visionRadius); System.Math.Pow((double) (xsquared + y * y), 0.5) <= visionRadius; y++)
                    {
                        if (entity.x + x >= 0 && entity.x + x < WIDTH && entity.y + y >= 0 && entity.y + y < HEIGHT)
                        {
                            if (foodMap[entity.x + x, entity.y + y] != null)
                            {
                                listState.Add((int) foodMap[entity.x + x, entity.y + y].foodValue);
                            }
                            else
                            {
                                listState.Add(0);
                            }
                        }
                        else
                        {
                            listState.Add(-1);
                        }
                    }
                }

                //handles allStates, ensuring no duplicates
                foreach (State state in allStates)
                {
                    bool equal = true;
                    for (int i = 0; i < listState.Count; i++)
                    {
                        if (listState[i] != state.stateItems[i])
                        {
                            equal = false;
                            break;
                        }
                    }
                    if (equal)
                    {
                        return state;
                    }
                }
                State newState = new State(listState);
                allStates.Add(newState);
                return newState;
            }

            private State(List<int> listState)
            {
                stateItems = listState;
            }

            public static int numberOfStates()
            {
                return allStates.Count;
            }
        }


        public AIMind1(int mindID, int characterID, CharacterAction[] actions)
        {
            this.mindID = mindID;
            this.characterID = characterID;
            this.stamina = ((Character)entities[characterID]).stamina;

            if (allThoughts.Count == 0)
            {
                foreach (CharacterAction action in actions)
                {
                    Thought t = new Thought();
                    t.action = action;
                    allThoughts.Add(t);
                }
            }

            for (int i = 0; i < SML; i++)
            {
                thoughtMemory[i] = new Thought();
            }
        }

        public override CharacterAction getNextAction()
        {
            //handle state updates, careful with the order
            staminaChange = ((Character)entities[characterID]).stamina - stamina;
            stamina = ((Character)entities[characterID]).stamina;
            State currentState = State.getState(entities[this.characterID], foodMap);
            //create staminaChange based memory associations
            for (int i = 1; i < SML; i++)
            {
                thoughtMemory[i].nextThoughts.bump(thoughtMemory[i - 1], staminaChange * System.Math.Pow(0.5, (double)i - 1.0));
            }
            for (int i = 0; i < SML; i++)
            {
                (State state, Thought thought) = stateMemory[i];
                if (state != null)
                {
                    state.nextThoughts.bump(thought, staminaChange * System.Math.Pow(0.5, (double)i));
                }
            }

            //add currentState's next thoughts to thought queue
            for (int i = 0; i < currentState.nextThoughts.count(); i++)
            {
                thoughtQueue.bump(currentState.nextThoughts.thoughtQueue[i], currentState.nextThoughts.priority[i]);
            }

            //refill thought queue
            thoughtQueue.bump(allThoughts[rand.Next(allThoughts.Count)], 0.1);

            //set picked action
            GameModel.CharacterAction? pickedAction = null;
            Thought lastThought = null;
            while (pickedAction == null)
            {
                //pop thought from thought queue
                Thought t = thoughtQueue.pop();
                //add thought to thoughtMemory
                for (int i = SML - 1; i > 0; i--)
                {
                    thoughtMemory[i] = thoughtMemory[i - 1];
                }
                thoughtMemory[0] = t;
                //add t's next thoughts to thought queue
                for (int i = 0; i < t.nextThoughts.count(); i++)
                {
                    thoughtQueue.bump(t.nextThoughts.thoughtQueue[i], t.nextThoughts.priority[i]);
                }
                //set picked action
                pickedAction = t.action;
                lastThought = t;
            }

            //add state to stateMemory
            for (int i = SML - 1; i > 0; i--)
            {
                stateMemory[i] = stateMemory[i - 1];
            }
            stateMemory[0] = (currentState, lastThought);

            //testing stuff
            Debug.Log("Number of states: " + State.numberOfStates().ToString());

            //return picked action
            return (GameModel.CharacterAction)pickedAction;
        }
    }
}
