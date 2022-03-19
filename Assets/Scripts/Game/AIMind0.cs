using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class GameModel
{
    private class AIMind0 : AIMind
    {
        //random
        System.Random rand = new System.Random();
        //thought list
        List<Thought> allThoughts = new List<Thought>();
        //thought priority queue
        ThoughtQueue thoughtQueue = new ThoughtQueue(10);
        //short term memory queue
        Thought[] shortMemory = new Thought[5];
        //most recent stamina change
        double staminaChange = 0.0;
        double stamina;


        //thoughts
        private class Thought
        {
            //character action
            public GameModel.CharacterAction? action = null;
            //next thoughts
            public ThoughtQueue nextThoughts = new ThoughtQueue(3);

            public Thought()
            {
                nextThoughts.bump(this, -1.0);
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


        public AIMind0(int mindID, int characterID, CharacterAction[] actions)
        {
            this.mindID = mindID;
            this.characterID = characterID;
            this.stamina = ((Character) entities[characterID]).stamina;

            if (allThoughts.Count == 0)
            {
                foreach (CharacterAction action in actions)
                {
                    Thought t = new Thought();
                    t.action = action;
                    allThoughts.Add(t);
                }
            }

            for (int i = 0; i < shortMemory.Length; i++)
            {
                shortMemory[i] = new Thought();
            }
        }

        public override CharacterAction getNextAction()
        {
            //handle state updates, careful with the order
            staminaChange = ((Character) entities[characterID]).stamina - stamina;
            stamina = ((Character) entities[characterID]).stamina;
            //create staminaChange based memory associations
            for (int i = 1; i < shortMemory.Length; i++)
            {
                shortMemory[i].nextThoughts.bump(shortMemory[i - 1], 1.0 * System.Math.Pow(0.5, (double) i - 1.0));
            }
            
            //refill thought queue
            thoughtQueue.bump(allThoughts[rand.Next(allThoughts.Count)], 0.1);

            //set picked action
            GameModel.CharacterAction? pickedAction = null;
            while (pickedAction == null)
            {
                //pop thought from thought queue
                Thought t = thoughtQueue.pop();
                //add thought to shortMemory
                for (int i = shortMemory.Length - 1; i > 0; i--)
                {
                    shortMemory[i] = shortMemory[i - 1];
                }
                shortMemory[0] = t;
                //add next thoughts to thought queue
                for (int i = 0; i < t.nextThoughts.count(); i++)
                {
                    thoughtQueue.bump(t.nextThoughts.thoughtQueue[i], t.nextThoughts.priority[i]);
                }
                //set picked action
                pickedAction = t.action;
            }
            //return picked action
            Debug.Log(pickedAction.ToString());
            return (GameModel.CharacterAction) pickedAction;
        }
    }
}
