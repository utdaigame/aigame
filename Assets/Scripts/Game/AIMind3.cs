using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class GameModel
{
    private class AIMind3 : AIMind
    {
        //Short Memory Length
        private const int SML = 3;
        private const double ADECAY = 0.01;
        private const double QDECAY = 0.8;
        private const int MAX_SECTION_STATES = 100;

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
        //most recent preference change
        double preferenceChange = 0.0;
        double previousPreference;
        double staminaChange = 0.0;
        double previousStamina;
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
            public AbsoluteThoughtQueue nextThoughts = new AbsoluteThoughtQueue(20);

            //previous thoughts for generalization / abstraction
            public List<Thought> previousThoughts = new List<Thought>();

            //root state for generalization / abstraction
            public State rootState = null;

            public Thought()
            {
                this.bump(this, 0.0);
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
            //access to characterID
            private int characterID;
            //stamina sections
            private int numSections = 1;
            private List<double> sectionDividers = new List<double>();
            private List<List<State>> sectionStates = new List<List<State>>();

            public State getState(Entity entity, Food[,] foodMap, double previousPreference)
            {
                List<int> listState = new List<int>();

                //stamina based state information
                double staminaValue = ((Character)entity).stamina;
                int sectionNum = numSections - 1;
                listState.Add(sectionNum);

                //vision based state information
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
                    for (int i = 1; i < listState.Count; i++)
                    {
                        if (listState[i] != state.stateItems[i])
                        {
                            equal = false;
                            break;
                        }
                    }
                    if (equal && staminaValue < 1.10 * state.stamina && staminaValue > 0.80 * state.stamina)
                    {
                        double staminaChange = staminaValue - state.stamina;
                        state.stamina += 0.9 * staminaChange;
                        state.preference += staminaChange;
                        for (int i = 0; i < sectionDividers.Count; i++)
                        {
                            if (state.stamina < sectionDividers[i])
                            {
                                sectionNum = i;
                                break;
                            }
                        }
                        int oldSectionNum = state.stateItems[0];
                        sectionStates[oldSectionNum].Remove(state);
                        state.stateItems[0] = sectionNum;
                        sectionStates[sectionNum].Add(state);
                        this.enforceSectionSize(sectionNum);
                        this.enforceSectionSize(oldSectionNum);
                        return state;
                    }
                }
                for (int i = 0; i < sectionDividers.Count; i++)
                {
                    if (staminaValue < sectionDividers[i])
                    {
                        sectionNum = i;
                        break;
                    }
                }
                listState[0] = sectionNum;
                State newState = new State(listState, allThoughts, (previousPreference + staminaValue) / 2, staminaValue);
                allStates.Add(newState);
                sectionStates[sectionNum].Add(newState);
                this.enforceSectionSize(sectionNum);
                int currentCount = allStates.Count;
                return newState;
            }

            //currently unused
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
                State newState = new State(listState, allThoughts, ((Character)entities[characterID]).stamina / ((Character)entities[characterID]).maxStamina, ((Character)entities[characterID]).stamina);
                allStates.Add(newState);
                return newState;
            }

            private void enforceSectionSize(int sectionNum)
            {
                if (sectionStates[sectionNum].Count >= MAX_SECTION_STATES)
                {
                    sectionStates.Insert(sectionNum, new List<State>());
                    if (sectionNum == 0)
                    {
                        if (sectionNum == numSections - 1)
                        {
                            sectionDividers.Add((((Character)entities[characterID]).minStamina + ((Character)entities[characterID]).maxStamina) / 2);
                        }
                        else
                        {
                            sectionDividers.Insert(sectionNum, (((Character)entities[characterID]).minStamina + sectionDividers[sectionNum]) / 2);
                        }
                    }
                    else if (sectionNum == numSections - 1)
                    {
                        sectionDividers.Add((sectionDividers[sectionNum - 1] + ((Character)entities[characterID]).maxStamina) / 2);
                    }
                    else
                    {
                        sectionDividers.Insert(sectionNum, (sectionDividers[sectionNum - 1] + sectionDividers[sectionNum]) / 2);
                    }
                    numSections++;
                    int i = 0;
                    while (i < sectionStates[sectionNum + 1].Count)
                    {
                        if (sectionStates[sectionNum + 1][i].stamina < sectionDividers[sectionNum])
                        {
                            sectionStates[sectionNum].Add(sectionStates[sectionNum + 1][i]);
                            sectionStates[sectionNum + 1].RemoveAt(i);
                        }
                        else
                        {
                            i++;
                        }
                    }
                    increaseStaminaSectionNumbers(sectionNum + 1);
                }
                else if (sectionStates[sectionNum].Count == 0)
                {
                    if (sectionNum == numSections - 1)
                    {
                        sectionDividers.RemoveAt(sectionNum - 1);
                    }
                    else
                    {
                        sectionDividers.RemoveAt(sectionNum);
                    }
                    sectionStates.RemoveAt(sectionNum);
                    numSections--;
                }
            }

            private void increaseStaminaSectionNumbers(int sectionNum)
            {
                while (sectionNum < numSections)
                {
                    foreach (State state in sectionStates[sectionNum])
                    {
                        state.stateItems[0] = sectionNum;
                    }
                    sectionNum++;
                }
            }

            public StateSpace(double visionRadius, List<Thought> allThoughts, int characterID)
            {
                this.visionRadius = visionRadius;
                this.allThoughts = allThoughts;
                this.sectionStates.Add(new List<State>());
                this.characterID = characterID;
            }

            public int numberOfStates()
            {
                return allStates.Count;
            }

            public int numberOfSections()
            {
                return numSections;
            }
        }


        private class State
        {
            //these ints are intended to be like custom enums, their values should be treated as symbolic
            public List<int> stateItems;
            //next thoughts
            public List<Thought> thoughts = new List<Thought>();
            //preference
            public double preference;
            //psuedo average stamina
            public double stamina;
            //stsTransform
            public StsTransform stsTransform;

            public State(List<int> listState, List<Thought> allThoughts, double preference, double stamina)
            {
                stateItems = listState;

                thoughts.Add(new Thought(this));
                allThoughts.Add(thoughts[0]);

                this.preference = preference;

                this.stamina = stamina;

                this.stsTransform = new StsTransform(this);
            }
        }


        private class StsTransform
        {
            private State startState;
            private List<List<(State, double)>> transform; //warning, these indexes are currently not consistent between different StsTransforms
            private List<Thought> triggers;

            public StsTransform(State startState)
            {
                this.startState = startState;
                this.transform = new List<List<(State, double)>>();
                this.triggers = new List<Thought>();
            }

            public void addTransform(Thought triggerThought, State triggeredState, double value)
            {
                int indexa = triggers.IndexOf(triggerThought);
                if (indexa == -1)
                {
                    //this piece is where the indexes could be made consistent by comparing to actionThoughts' indexing
                    indexa = transform.Count;
                    triggers.Add(triggerThought);
                    transform.Add(new List<(State, double)>());
                }
                List<(State, double)> stateList = transform[indexa];
                int indexs = StsTransform.indexOfStatePair(stateList, triggeredState);
                if (indexs == -1)
                {
                    indexs = stateList.Count;
                    stateList.Add((triggeredState, 0.0));
                }
                stateList[indexs] = (stateList[indexs].Item1, stateList[indexs].Item2 + value);
            }

            private static int indexOfStatePair(List<(State, double)> stateList, State state)
            {
                for (int i = 0; i < stateList.Count; i++)
                {
                    if (state == stateList[i].Item1)
                    {
                        return i;
                    }
                }
                return -1;
            }

            public (Thought, double) getBestThought(int maxTraversalSteps = 100)
            {
                (Thought thought, double support, int cs) = this.getBestThoughtInternal(maxTraversalSteps, 0);
                return (thought, support);
            }

            private (Thought, double, int) getBestThoughtInternal(int maxTraversalSteps = 100, int currentStep = 0)
            {
                if (currentStep >= maxTraversalSteps)
                {
                    return (null, 0, currentStep);
                }
                Thought thought = null;
                double support = 0.0;
                List<(Thought, State, double)> nextTransforms = new List<(Thought, State, double)>();
                int indexa = 0;
                int indexs = 0;
                while (currentStep < maxTraversalSteps && indexa < this.triggers.Count)
                {
                    if (indexs < transform[indexa].Count)
                    {
                        nextTransforms.Add((triggers[indexa], transform[indexa][indexs].Item1, transform[indexa][indexs].Item2));
                        indexs++;
                        currentStep++;
                    }
                    else
                    {
                        indexa++;
                        indexs = 0;
                    }
                }
                double nextSupport = 0.0;
                //int? besti = null;
                for (int i = 0; i < nextTransforms.Count; i++)
                {
                    (Thought trigger, State nextState, double value) = nextTransforms[i];
                    //this creates an approximation of breadth first search
                    (Thought nt, double ns, int cs) = nextState.stsTransform.getBestThoughtInternal(maxTraversalSteps, currentStep + (int)((maxTraversalSteps - currentStep) * (nextTransforms.Count - i - 1) / nextTransforms.Count));
                    currentStep = cs - (int)((maxTraversalSteps - currentStep) * (nextTransforms.Count - i - 1) / nextTransforms.Count);
                    if (thought == null)
                    {
                        thought = trigger;
                        support = value;
                        nextSupport = ns;
                        //besti = i;
                    }
                    else if (value + ns > support + nextSupport)
                    {
                        thought = trigger;
                        support = value;
                        nextSupport = ns;
                        //besti = i;
                    }
                }
                //push values backwords to show that a state's value is influenced by future states' values
                //if (besti != null)
                //{
                //    this.addTransform(thought, nextTransforms[besti.Value].Item2, 0.8 * (nextSupport - support));
                //}

                return (thought, support + nextSupport, currentStep);
            }
        }


        public AIMind3(int mindID, int characterID, CharacterAction[] actions)
        {
            this.mindID = mindID;
            this.characterID = characterID;
            this.stateSpace = new StateSpace(((Character)entities[characterID]).visionRange, allThoughts, characterID);

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

            stateMemory[0] = (stateSpace.getState(entities[this.characterID], foodMap, ((Character)entities[this.characterID]).stamina), new Thought());
            this.previousPreference = stateMemory[0].Item1.preference;
            this.previousStamina = ((Character)entities[this.characterID]).stamina;
        }

        public override CharacterAction getNextAction()
        {
            //handle state updates, careful with the order
            previousPreference = stateMemory[0].Item1.preference;
            State currentState = stateSpace.getState(entities[this.characterID], foodMap, previousPreference);
            preferenceChange = currentState.preference - previousPreference;
            staminaChange = ((Character)entities[this.characterID]).stamina - previousStamina;

            //create staminaChange based memory associations
            double ataImportanceFactor = 0.01;
            double staImportanceFactor = 0.5;
            //double stsImportanceFactor = 0.001;
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
                            //t1.bump(t0, stsImportanceFactor * preferenceChange * System.Math.Pow(0.5, (double)i - 1.0));
                            t1.rootState.preference += 0.01 * t0.rootState.preference * System.Math.Pow(0.5, (double)i - 1.0);
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

            //this is where it looks into the future via StsTransforms
            stateMemory[0].Item1.stsTransform.addTransform(stateMemory[0].Item2, currentState, staminaChange);
            (Thought transformThought, double transformWeight) = currentState.stsTransform.getBestThought();
            if (transformThought != null)
            {
                thoughtQueue.bump(transformThought, transformWeight);
            }

            //advance thoughtMemory
            for (int i = SML - 1; i > 0; i--)
            {
                thoughtMemory[i] = thoughtMemory[i - 1];
            }
            thoughtMemory[0] = new List<Thought>();

            //run currentState's thoughts, to add action thoughts to the main priority queue
            foreach (Thought thought in currentState.thoughts)
            {
                runThought(thought);
            }

            //refill thought queue
            thoughtQueue.bump(actionThoughts[rand.Next(actionThoughts.Count)], 0.001);

            //set picked action
            GameModel.CharacterAction? pickedAction = null;
            Thought lastThought = null;
            double weight = 0.0; // <-- testing
            while (pickedAction == null)
            {
                weight = thoughtQueue.priority[0]; // <-- testing
                //pop thought from thought queue
                Thought t = thoughtQueue.pop();
                //set picked action
                pickedAction = runThought(t);
                lastThought = t;
            }

            //add state to stateMemory
            for (int i = SML - 1; i > 0; i--)
            {
                stateMemory[i] = stateMemory[i - 1];
            }
            stateMemory[0] = (currentState, lastThought);

            //testing stuff
            //Debug.Log("Weight of action thought: " + weight.ToString());
            //Debug.Log("Number of states, sections: " + stateSpace.numberOfStates().ToString() + ", " + stateSpace.numberOfSections().ToString());

            //decay anything left in the thought priority queue
            thoughtQueue.decay(QDECAY);
            previousStamina = ((Character)entities[this.characterID]).stamina;

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
