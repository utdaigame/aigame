using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class GameModel
{
    private class AIMind2 : AIMind
    {
        //Short Memory Length
        private const int SML = 5;
        private const double ADECAY = 0.01;
        private const double QDECAY = 0.8;

        //random
        System.Random rand = new System.Random();
        //action thoughts
        List<Thought> actionThoughts = new List<Thought>();
        //thought list
        List<Thought> allThoughts = new List<Thought>();
        //thought priority queue
        ThoughtQueue thoughtQueue = new ThoughtQueue(20);
        //short term memory queue
        List<Thought>[] thoughtMemory = new List<Thought>[SML];
        //most recent stamina change
        double staminaChange = 0.0;
        double stamina;
        //short term state memory
        (State, Thought)[] stateMemory = new(State, Thought)[SML];
        //state space
        private StateSpace stateSpace;
        
        
        //thoughts
        private class Thought
        {
            //character action
            public GameModel.CharacterAction? action = null;
            //next thoughts
            public AbsoluteThoughtQueue nextThoughts = new AbsoluteThoughtQueue(10);

            //previous thoughts for generalization / abstraction
            public List<Thought> previousThoughts = new List<Thought>();

            //root state for generalization / abstraction
            public State rootState = null;

            public Thought()
            {
                this.bump(this, -0.1);
            }
            public Thought(State rootState)
            {
                this.rootState = rootState;
                this.bump(this, 0.0);
            }

            public void bump(Thought thought, double weight)
            {
                //do the bump
                (Thought rt, double rw) = nextThoughts.bump(thought, weight);
                //handle removal
                if (rt != null)
                {
                    rt.previousThoughts.Remove(this);
                }
                //add self to previous
                if (!thought.previousThoughts.Contains(this))
                {
                    thought.previousThoughts.Add(this);
                }
            }
        }
        //thought absolute priority queue class - sorts by magnitude - no pop method
        private class AbsoluteThoughtQueue
        {
            public List<Thought> thoughtQueue;
            public List<double> priority;
            private int maxLength;

            public AbsoluteThoughtQueue(int maxLength)
            {
                thoughtQueue = new List<Thought>();
                priority = new List<double>();
                this.maxLength = maxLength;
            }

            //bump a thought
            public (Thought, double) bump(Thought t, double weight)
            {
                //decay
                this.decay(ADECAY);

                //amplify negatives
                //if (weight < 0) weight *= 1.5;

                if (!thoughtQueue.Contains(t))
                {
                    //find index of first lower priority thought
                    int count = thoughtQueue.Count;
                    int index = 0;
                    while (index < count)
                    {
                        if (System.Math.Abs(priority[index]) <= System.Math.Abs(weight)) break;
                        index++;
                    }
                    thoughtQueue.Insert(index, t);
                    priority.Insert(index, weight);

                    //enforce maxLength
                    if (thoughtQueue.Count > maxLength)
                    {
                        Thought removedThought = thoughtQueue[maxLength];
                        double removedWeight = priority[maxLength];
                        thoughtQueue.RemoveAt(maxLength);
                        priority.RemoveAt(maxLength);
                        return (removedThought, removedWeight);
                    }
                }
                else
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
                        if (System.Math.Abs(priority[index]) <= System.Math.Abs(weight)) break;
                        index++;
                    }
                    thoughtQueue.Insert(index, t);
                    priority.Insert(index, newPriority);
                }
                return (null, 0);
            }
            //get the count of the number of thoughts
            public int count()
            {
                return thoughtQueue.Count;
            }
            //reduce all weights by some value between 0-1
            private void decay(double decayValue)
            {
                if (decayValue <= 0 || decayValue > 1)
                {
                    return;
                }
                for (int i = 0; i < priority.Count; i++)
                {
                    priority[i] *= 1 - decayValue;
                }
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
                else
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
            //reduce all weights by some value between 0-1
            public void decay(double decayValue)
            {
                if (decayValue <= 0 || decayValue > 1)
                {
                    return;
                }
                for (int i = 0; i < priority.Count; i++)
                {
                    priority[i] *= 1 - decayValue;
                }
            }
        }


        private class StateSpace
        {
            //state list
            private List<State> allStates = new List<State>();
            //visionRadius
            private double visionRadius;
            //access to allThoughts
            private List<Thought> allThoughts;

            public State getState(Entity entity, Food[,] foodMap)
            {
                List<int> listState = new List<int>();
                for (int x = (int)(-visionRadius); (double)x <= visionRadius; x++)
                {
                    int xsquared = x * x;
                    for (int y = (int)(-visionRadius); System.Math.Pow((double)(xsquared + y * y), 0.5) <= visionRadius; y++)
                    {
                        if (entity.x + x >= 0 && entity.x + x < WIDTH && entity.y + y >= 0 && entity.y + y < HEIGHT)
                        {
                            if (foodMap[entity.x + x, entity.y + y] != null)
                            {
                                listState.Add((int)foodMap[entity.x + x, entity.y + y].foodValue);
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
                State newState = new State(listState, allThoughts);
                allStates.Add(newState);
                int currentCount = allStates.Count;
                for (int i = 0; i < currentCount && i < 10; i++)
                {
                    getOverlapState(newState, allStates[i]);
                }
                return newState;
            }

            public State getOverlapState(State state1, State state2)
            {
                List<int> listState = new List<int>();
                for (int i = 0; i < state1.stateItems.Count; i++)
                {
                    if (state1.stateItems[i] == state2.stateItems[i])
                    {
                        listState.Add(state1.stateItems[i]);
                    }
                    else if (state1.stateItems[i] < 0 || state2.stateItems[i] < 0)
                    {
                        listState.Add(-1);
                    }
                    else
                    {
                        listState.Add(0);
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
                        //populate thoughts
                        foreach (Thought thought in state.thoughts)
                        {
                            if (!state1.thoughts.Contains(thought))
                            {
                                state1.thoughts.Add(thought);
                            }
                            if (!state2.thoughts.Contains(thought))
                            {
                                state2.thoughts.Add(thought);
                            }
                        }

                        return state;
                    }
                }
                State newState = new State(listState, allThoughts);
                allStates.Add(newState);
                return newState;
            }

            public StateSpace(double visionRadius, List<Thought> allThoughts)
            {
                this.visionRadius = visionRadius;
                this.allThoughts = allThoughts;
            }

            public int numberOfStates()
            {
                return allStates.Count;
            }
        }


        private class State
        {
            //these ints are intended to be like custom enums, their values should be treated as symbolic
            public List<int> stateItems;
            //next thoughts
            public List<Thought> thoughts = new List<Thought>();

            public State(List<int> listState, List<Thought> allThoughts)
            {
                stateItems = listState;
                thoughts.Add(new Thought(this));
                allThoughts.Add(thoughts[0]);
            }
        }


        public AIMind2(int mindID, int characterID, CharacterAction[] actions)
        {
            this.mindID = mindID;
            this.characterID = characterID;
            this.stamina = ((Character)entities[characterID]).stamina;
            this.stateSpace = new StateSpace(((Character)entities[characterID]).visionRange, allThoughts);

            if (actionThoughts.Count == 0)
            {
                foreach (CharacterAction action in actions)
                {
                    Thought t = new Thought();
                    t.action = action;
                    actionThoughts.Add(t);
                }
            }

            for (int i = 0; i < SML; i++)
            {
                thoughtMemory[i] = new List<Thought>();
            }
        }

        public override CharacterAction getNextAction()
        {
            //handle state updates, careful with the order
            staminaChange = ((Character)entities[characterID]).stamina - stamina;
            stamina = ((Character)entities[characterID]).stamina;
            State currentState = stateSpace.getState(entities[this.characterID], foodMap);

            //create staminaChange based memory associations
            double ataImportanceFactor = 0.5;
            double staImportanceFactor = 1;
            double stsImportanceFactor = 0.01;
            for (int i = 1; i < SML; i++)
            {
                foreach (Thought t1 in thoughtMemory[i])
                {
                    foreach (Thought t0 in thoughtMemory[i - 1])
                    {
                        if (t0.rootState == null)
                        {
                            if (t1.rootState == null)
                            {
                                t1.bump(t0, ataImportanceFactor * staminaChange * System.Math.Pow(0.5, (double)i - 1.0));
                            }
                        }
                        else if (t1.rootState != null)
                        {
                            t1.bump(t0, stsImportanceFactor * staminaChange * System.Math.Pow(0.5, (double)i - 1.0));
                        }
                    }
                }
            }
            for (int i = 0; i < SML; i++)
            {
                foreach (Thought t1 in thoughtMemory[i])
                {
                    foreach (Thought t2 in thoughtMemory[i])
                    {
                        if (t1.rootState != null && t2.rootState == null)
                        {
                            t1.bump(t2, staImportanceFactor * staminaChange * System.Math.Pow(0.5, (double)i - 1.0));
                        }
                    }
                }
            }
            
            //advance thoughtMemory
            for (int i = SML - 1; i > 0; i--)
            {
                thoughtMemory[i] = thoughtMemory[i - 1];
            }
            thoughtMemory[0] = new List<Thought>();

            //run currentState's thoughts
            foreach (Thought thought in currentState.thoughts)
            {
                runThought(thought);
            }

            //refill thought queue
            thoughtQueue.bump(actionThoughts[rand.Next(actionThoughts.Count)], 0.1);

            //set picked action
            GameModel.CharacterAction? pickedAction = null;
            Thought lastThought = null;
            while (pickedAction == null)
            {
                //pop thought from thought queue
                Thought t = thoughtQueue.pop();
                //set picked action
                pickedAction = runThought(t);
                lastThought = t;
            }

            //add state to stateMemory  <-- currenntly unneeded >-<>-<>-<
            for (int i = SML - 1; i > 0; i--)
            {
                stateMemory[i] = stateMemory[i - 1];
            }
            stateMemory[0] = (currentState, lastThought);

            //testing stuff
            Debug.Log("Number of states: " + stateSpace.numberOfStates().ToString());

            //decay anything left in the thought priority queue
            thoughtQueue.decay(QDECAY);

            //return picked action
            return (GameModel.CharacterAction)pickedAction;
        }

        private GameModel.CharacterAction? runThought(Thought t)
        {
            //add thought to thoughtMemory
            thoughtMemory[0].Add(t);
            //add t's next thoughts to thought queue
            for (int i = 0; i < t.nextThoughts.count(); i++)
            {
                thoughtQueue.bump(t.nextThoughts.thoughtQueue[i], t.nextThoughts.priority[i]);
            }
            return t.action;
        }
    }
}
