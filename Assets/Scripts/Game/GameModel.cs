using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

[RequireComponent(typeof(GameRenderer))]
public partial class GameModel : MonoBehaviour
{
    private static int nextID;
    private static int nextMindID;

    private static Food[,] foodMap;
    private static Character[,] characterMap;
    private static List<Entity> entities;
    private static List<AIMind> minds;
    private static List<RenderActionPair> actionList;

    public const int WIDTH = 4;
    public const int HEIGHT = 4;
    public const double baseCost = 0.02;

    //random
    System.Random rand = new System.Random();
    int frameNumber = 0;

    private GameRenderer gameRenderer;

    public int generateNextID()
    {
        int tmp = nextID;
        nextID++;
        return tmp;
    }
    public int generateNextMindID()
    {
        int tmp = nextMindID;
        nextMindID++;
        return tmp;
    }

    // enum and structure for accessing character actions
    public enum CharacterAction{MoveNorth, MoveSouth, MoveEast, MoveWest, PickUp, DoNothing};
    public Action<Character>[] actions =
        {
            //MoveNorth
            (c)=>
            {
                c.stamina -= 0.1 + baseCost;
                if (c.y < HEIGHT - 1 && characterMap[c.x, c.y + 1] == null)
                {
                    characterMap[c.x, c.y] = null;
                    c.y += 1;
                    characterMap[c.x, c.y] = c;
                }
                c.enforceMMStamina();
                //for rendering
                actionList.Add(new RenderActionPair(c.ID, RenderAction.Move));
            },
            //MoveSouth
            (c)=>
            {
                c.stamina -= 0.1 + baseCost;
                if (c.y > 0 && characterMap[c.x, c.y - 1] == null)
                {
                    characterMap[c.x, c.y] = null;
                    c.y -= 1;
                    characterMap[c.x, c.y] = c;
                }
                c.enforceMMStamina();
                //for rendering
                actionList.Add(new RenderActionPair(c.ID, RenderAction.Move));
            },
            //MoveEast
            (c)=>
            {
                c.stamina -= 0.1 + baseCost;
                if (c.x < WIDTH - 1 && characterMap[c.x + 1, c.y] == null)
                {
                    characterMap[c.x, c.y] = null;
                    c.x += 1;
                    characterMap[c.x, c.y] = c;
                }
                c.enforceMMStamina();
                //for rendering
                actionList.Add(new RenderActionPair(c.ID, RenderAction.Move));
            },
            //MoveWest
            (c)=>
            {
                c.stamina -= 0.1 + baseCost;
                if (c.x > 0 && characterMap[c.x - 1, c.y] == null)
                {
                    characterMap[c.x, c.y] = null;
                    c.x -= 1;
                    characterMap[c.x, c.y] = c;
                }
                c.enforceMMStamina();
                //for rendering
                actionList.Add(new RenderActionPair(c.ID, RenderAction.Move));
            },
            //PickUp
            (c)=>
            {
                c.stamina -= 0.05 + baseCost;
                if (foodMap[c.x, c.y] != null)
                {
                    c.stamina += foodMap[c.x, c.y].foodValue;
                    foodMap[c.x, c.y].tags.Add("Removed");
                    //for rendering
                    actionList.Add(new RenderActionPair(foodMap[c.x, c.y].ID, RenderAction.Eaten));
                    ///this line generates a warning, because it is called while an enumeration of entities is active. It does not actually break though. 
                    //entities[foodMap[c.x, c.y].ID] = null;
                    foodMap[c.x, c.y] = null;
                }
                c.enforceMMStamina();
                //for rendering
                actionList.Add(new RenderActionPair(c.ID, RenderAction.PickUp));
            },
            //DoNothing
            (c)=>
            {
                c.stamina -= baseCost;
                c.enforceMMStamina();
            }
        };

    public enum RenderAction{ Move, PickUp, Add, Remove, Eaten }; // <-- Remove is currently unused
    public class RenderActionPair
    {
        //Character performing action
        public int ID;

        //Action being performed
        public RenderAction renderAction;

        //Will display the pair action pair when toString is called
        public override string ToString()
        {
            return "(" + ID + ", " + renderAction + ")";
        }

        public RenderActionPair(int id, RenderAction ra)
        {
            ID = id;
            renderAction = ra;
        }
    }

    public abstract class Entity
    {
        //ID
        public int ID;
        //Name
        public string name;
        //Tags
        public List<string> tags;
        //Position
        public int x;
        public int y;
    }
    public class Food : Entity
    {
        //Food Value
        public double foodValue;

        public Food(int x, int y, int id)
        {
            this.x = x;
            this.y = y;
            this.ID = id;
            this.name = "notaname";
            this.foodValue = 2.0;
            this.tags = new List<string> {"Food"};
            //for rendering
            actionList.Add(new RenderActionPair(id, RenderAction.Add));
        }
        public Food(int x, int y, int id, string name, double foodValue)
        {
            this.x = x;
            this.y = y;
            this.ID = id;
            this.name = name;
            this.foodValue = foodValue;
            this.tags = new List<string> { "Food" };
            //for rendering
            actionList.Add(new RenderActionPair(id, RenderAction.Add));
        }
    }
    public class Character : Entity
    {
        //Mind ID
        public int mindID;
        //Stamina
        public double stamina;
        public double maxStamina;
        //Actions
        public CharacterAction[] assignedActions;

        public Character(int x, int y, int id, int mindID)
        {
            this.x = x;
            this.y = y;
            this.ID = id;
            this.mindID = mindID;
            this.name = "notaname";
            this.stamina = 5.0;
            this.maxStamina = 10.0;
            this.assignedActions = new CharacterAction[] { CharacterAction.DoNothing, CharacterAction.MoveEast, CharacterAction.MoveNorth, CharacterAction.MoveSouth, CharacterAction.MoveWest, CharacterAction.PickUp };
            this.tags = new List<string> { "Character" };
            //for rendering
            actionList.Add(new RenderActionPair(id, RenderAction.Add));
        }
        public Character(int x, int y, int id, int mindID, string name)
        {
            this.x = x;
            this.y = y;
            this.ID = id;
            this.mindID = mindID;
            this.name = name;
            this.stamina = 10.0;
            this.maxStamina = 10.0;
            this.assignedActions = new CharacterAction[] { CharacterAction.DoNothing, CharacterAction.MoveEast, CharacterAction.MoveNorth, CharacterAction.MoveSouth, CharacterAction.MoveWest, CharacterAction.PickUp };
            this.tags = new List<string> { "Character" };
            //for rendering
            actionList.Add(new RenderActionPair(id, RenderAction.Add));
        }
        public Character(int x, int y, int id, int mindID, string name, double startingStamina, double maxStamina, CharacterAction[] actions)
        {
            this.x = x;
            this.y = y;
            this.ID = id;
            this.mindID = mindID;
            this.name = name;
            this.stamina = startingStamina;
            this.maxStamina = maxStamina;
            this.assignedActions = actions;
            this.tags = new List<string> { "Character" };
            //for rendering
            actionList.Add(new RenderActionPair(id, RenderAction.Add));
        }

        public void enforceMMStamina()
        {
            if (stamina < 0.0) stamina = 0.0;
            if (stamina > maxStamina) stamina = maxStamina;
        }
    }
    public abstract class AIMind
    {
        //Mind ID
        protected int mindID;
        //Entity ID
        protected int characterID;
        //get next action
        public abstract CharacterAction getNextAction();
    }
    private class GreedySearchAIMind : AIMind
    {
        public GreedySearchAIMind(int mindID, int characterID)
        {
            this.mindID = mindID;
            this.characterID = characterID;
        }

        public override CharacterAction getNextAction()
        {
            if (foodMap[entities[characterID].x, entities[characterID].y] != null)
            {
                return CharacterAction.PickUp;
            }

            Food closestFood = null;
            int minManhattanDistance = -1;
            for (int cx = 0; cx < WIDTH; cx++)
            {
                for (int cy = 0; cy < HEIGHT; cy++)
                {
                    if (foodMap[cx, cy] != null)
                    {
                        int manhattanDistance = Math.Abs(cx - entities[characterID].x) + Math.Abs(cy - entities[characterID].y);
                        if (closestFood == null || manhattanDistance < minManhattanDistance)
                        {
                            closestFood = foodMap[cx, cy];
                            minManhattanDistance = manhattanDistance;
                        }
                    }
                }
            }

            if (closestFood == null)
            {
                return CharacterAction.DoNothing;
            }
            else if (closestFood.x < entities[characterID].x)
            {
                return CharacterAction.MoveWest;
            }
            else if (closestFood.y < entities[characterID].y)
            {
                return CharacterAction.MoveSouth;
            }
            else if (closestFood.x > entities[characterID].x)
            {
                return CharacterAction.MoveEast;
            }
            else
            {
                return CharacterAction.MoveNorth;
            }
        }
    }
    
    // Start is called before the first frame update
    void Start()
    {
        gameRenderer = GetComponent<GameRenderer>();

        nextID = 0;
        nextMindID = 0;
        characterMap = new Character[WIDTH, HEIGHT];
        foodMap = new Food[WIDTH, HEIGHT];
        entities = new List<Entity>();
        minds = new List<AIMind>();
        actionList = new List<RenderActionPair>();

        int firstEntityID = generateNextID();
        int firstMindID = generateNextMindID();
        entities.Insert(firstEntityID, new Character(WIDTH / 2, HEIGHT / 2, firstEntityID, firstMindID, name = "Gree"));
        minds.Insert(firstMindID, new AIMind0(firstMindID, firstEntityID, ((Character) entities[firstEntityID]).assignedActions));
        characterMap[WIDTH / 2, HEIGHT / 2] = (Character) entities[firstEntityID];

        int foodID = generateNextID();
        entities.Insert(foodID, new Food(1, 0, foodID));
        foodMap[1, 0] = (Food) entities[foodID];
    }

    // FixedUpdate is called once per game frame
    void FixedUpdate()
    {
        tmpDisplay();
        frameNumber++;
        if (frameNumber % 5 == 0)
        {
            int nfx = rand.Next(WIDTH);
            int nfy = rand.Next(HEIGHT);
            if (foodMap[nfx, nfy] == null)
            {
                int foodID = generateNextID();
                entities.Insert(foodID, new Food(nfx, nfy, foodID));
                foodMap[nfx, nfy] = (Food)entities[foodID];
            }
        }
        foreach (Entity entity in entities)
        {
            if (entity != null)
            {
                if (entity.tags.Contains("Character"))
                {
                    Debug.Log(((Character) entity).stamina);
                    Action<Character> action = actions[(int)minds[((Character)entity).mindID].getNextAction()];
                    action((Character)entity);
                }
            }
        }
        tmpDisplay();

        gameRenderer.MyUpdate(entities);
        actionList.Clear();
    }

    private void tmpDisplay()
    {
        // temporary display
        string s = "";
        for (int i = 0; i < WIDTH; i++)
        {
            for (int j = 0; j < HEIGHT; j++)
            {
                string f = "";
                if (foodMap[i, j] != null)
                {
                    f = foodMap[i, j].foodValue + "";
                }
                else
                {
                    f = "0";
                }
                string c = "";
                if (characterMap[i, j] != null)
                {
                    c = characterMap[i, j].name;
                }
                s += f + c + "\t";
            }
            s += "\n";
        }
        Debug.Log(s);
    }
}
