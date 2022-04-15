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

    public const int WIDTH = 8;
    public const int HEIGHT = 8;
    public const double baseCost = 0.01;

    private static int[] iMove = new int[1000];
    private static int iMoveNum = 0;
    private static int[] iPickup = new int[1000];
    private static int iPickupNum = 0;

    //random
    System.Random rand = new System.Random();
    private static int frameNumber = 0;

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
                c.healthRegen();
                c.stamina -= 0.1 + baseCost;
                if (c.y < HEIGHT - 1 && characterMap[c.x, c.y + 1] == null)
                {
                    characterMap[c.x, c.y] = null;
                    c.y += 1;
                    characterMap[c.x, c.y] = c;
                }
                else
                {
                    c.health -= 0.5;
                    c.enforceMMHealth();
                    iMove[frameNumber % 1000] += 1;
                    iMoveNum++;
                }
                c.enforceMMStamina();
                //for rendering
                actionList.Add(new RenderActionPair(c.ID, RenderAction.Move));
            },
            //MoveSouth
            (c)=>
            {
                c.healthRegen();
                c.stamina -= 0.1 + baseCost;
                if (c.y > 0 && characterMap[c.x, c.y - 1] == null)
                {
                    characterMap[c.x, c.y] = null;
                    c.y -= 1;
                    characterMap[c.x, c.y] = c;
                }
                else
                {
                    c.health -= 0.5;
                    c.enforceMMHealth();
                    iMove[frameNumber % 1000] += 1;
                    iMoveNum++;
                }
                c.enforceMMStamina();
                //for rendering
                actionList.Add(new RenderActionPair(c.ID, RenderAction.Move));
            },
            //MoveEast
            (c)=>
            {
                c.healthRegen();
                c.stamina -= 0.1 + baseCost;
                if (c.x < WIDTH - 1 && characterMap[c.x + 1, c.y] == null)
                {
                    characterMap[c.x, c.y] = null;
                    c.x += 1;
                    characterMap[c.x, c.y] = c;
                }
                else
                {
                    c.health -= 0.5;
                    c.enforceMMHealth();
                    iMove[frameNumber % 1000] += 1;
                    iMoveNum++;
                }
                c.enforceMMStamina();
                //for rendering
                actionList.Add(new RenderActionPair(c.ID, RenderAction.Move));
            },
            //MoveWest
            (c)=>
            {
                c.healthRegen();
                c.stamina -= 0.1 + baseCost;
                if (c.x > 0 && characterMap[c.x - 1, c.y] == null)
                {
                    characterMap[c.x, c.y] = null;
                    c.x -= 1;
                    characterMap[c.x, c.y] = c;
                }
                else
                {
                    c.health -= 0.5;
                    c.enforceMMHealth();
                    iMove[frameNumber % 1000] += 1;
                    iMoveNum++;
                }
                c.enforceMMStamina();
                //for rendering
                actionList.Add(new RenderActionPair(c.ID, RenderAction.Move));
            },
            //PickUp
            (c)=>
            {
                c.healthRegen();
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
                else
                {
                    c.health -= 0.5;
                    c.enforceMMHealth();
                    iPickup[frameNumber % 1000] += 1;
                    iPickupNum++;
                }
                c.enforceMMStamina();
                //for rendering
                actionList.Add(new RenderActionPair(c.ID, RenderAction.PickUp));
            },
            //DoNothing
            (c)=>
            {
                c.healthRegen();
                c.healthRegen();
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
            this.foodValue = 1.0;
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
        public double minStamina;
        public double maxStamina;
        public double lowestStamina;
        //health
        public double health;
        public double minHealth;
        public double maxHealth;
        public double lowestHealth;
        //Senses
        public double visionRange;
        //Actions
        public CharacterAction[] assignedActions;

        public Character(int x, int y, int id, int mindID)
        {
            this.x = x;
            this.y = y;
            this.ID = id;
            this.mindID = mindID;
            this.name = "notaname";
            this.stamina = 50.0;
            this.minStamina = -100.0;
            this.maxStamina = 100.0;
            this.lowestStamina = this.stamina;
            this.health = 10.0;
            this.minHealth = 0.0;
            this.maxHealth = 10.0;
            this.lowestHealth = this.health;
            this.visionRange = 1.5;
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
            this.stamina = 50.0;
            this.minStamina = -100.0;
            this.maxStamina = 100.0;
            this.lowestStamina = this.stamina;
            this.health = 10.0;
            this.minHealth = 0.0;
            this.maxHealth = 10.0;
            this.lowestHealth = this.health;
            this.visionRange = 1.5;
            this.assignedActions = new CharacterAction[] { CharacterAction.DoNothing, CharacterAction.MoveEast, CharacterAction.MoveNorth, CharacterAction.MoveSouth, CharacterAction.MoveWest, CharacterAction.PickUp };
            this.tags = new List<string> { "Character" };
            //for rendering
            actionList.Add(new RenderActionPair(id, RenderAction.Add));
        }
        public Character(int x, int y, int id, int mindID, string name, double startingStamina, double minStamina, double maxStamina,
            double startingHealth, double minHealth, double maxHealth, double visionRange, CharacterAction[] actions)
        {
            this.x = x;
            this.y = y;
            this.ID = id;
            this.mindID = mindID;
            this.name = name;
            this.stamina = startingStamina;
            this.minStamina = minStamina;
            this.maxStamina = maxStamina;
            this.lowestStamina = this.stamina;
            this.health = startingHealth;
            this.minHealth = minHealth;
            this.maxHealth = maxHealth;
            this.lowestHealth = this.health;
            this.visionRange = visionRange;
            this.assignedActions = actions;
            this.tags = new List<string> { "Character" };
            //for rendering
            actionList.Add(new RenderActionPair(id, RenderAction.Add));
        }

        public void enforceMMStamina()
        {
            if (stamina < minStamina) stamina = minStamina;
            if (stamina > maxStamina) stamina = maxStamina;

            //update lowestStamina
            if (stamina < lowestStamina) lowestStamina = stamina;
        }

        public void enforceMMHealth()
        {
            if (health < minHealth) health = minHealth;
            if (health > maxHealth) health = maxHealth;

            //update lowestHealth
            if (health < lowestHealth) lowestHealth = health;
        }

        public void healthRegen()
        {
            health += 0.1;
            enforceMMHealth();
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
            double minDistance = -1;
            for (int cx = (int)(-((Character)entities[characterID]).visionRange); (double)cx <= ((Character)entities[characterID]).visionRange; cx++)
            {
                int xsquared = cx * cx;
                for (int cy = (int)(-((Character)entities[characterID]).visionRange); System.Math.Pow((double)(xsquared + cy * cy), 0.5) <= ((Character)entities[characterID]).visionRange; cy++)
                {
                    if (foodMap[cx, cy] != null)
                    {
                        double distance = Math.Sqrt(Math.Pow(cx - entities[characterID].x, 2.0) + Math.Pow(cy - entities[characterID].y, 2.0));
                        if (closestFood == null || distance < minDistance)
                        {
                            closestFood = foodMap[cx, cy];
                            minDistance = distance;
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
        entities.Insert(firstEntityID, new Character(WIDTH / 2, HEIGHT / 2, firstEntityID, firstMindID, name = "AIM3"));
        minds.Insert(firstMindID, new AIMind3(firstMindID, firstEntityID, ((Character) entities[firstEntityID]).assignedActions));
        characterMap[WIDTH / 2, HEIGHT / 2] = (Character) entities[firstEntityID];

        //int entityID = generateNextID();
        //int mindID = generateNextMindID();
        //entities.Insert(entityID, new Character(1, 1, entityID, mindID, name = "AIM2"));
        //minds.Insert(mindID, new AIMind1(mindID, entityID, ((Character)entities[entityID]).assignedActions));
        //characterMap[1, 1] = (Character)entities[entityID];

        //spawn starting food
        for (int i = 0; i < WIDTH * HEIGHT / 4; i++)
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
    }

    // FixedUpdate is called once per game frame
    void FixedUpdate()
    {
        //tmpDisplay();
        frameNumber++;
        iMoveNum -= iMove[frameNumber % 1000];
        iPickupNum -= iPickup[frameNumber % 1000];
        iMove[frameNumber % 1000] = 0;
        iPickup[frameNumber % 1000] = 0;
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
                    Debug.Log("Stamina: " + ((Character) entity).stamina.ToString());
                    Debug.Log("Lowest Stamina: " + ((Character) entity).lowestStamina.ToString());
                    Debug.Log("Health: " + ((Character)entity).health.ToString());
                    Debug.Log("Lowest Health: " + ((Character)entity).lowestHealth.ToString());
                    CharacterAction pickedAction = minds[((Character)entity).mindID].getNextAction();
                    //Debug.Log(pickedAction.ToString());
                    Action<Character> action = actions[(int)pickedAction];
                    action((Character)entity);
                }
            }
        }
        Debug.Log("Impossible moves attempted in last 1000: " + iMoveNum.ToString());
        Debug.Log("Impossible pickups attempted in last 1000: " + iPickupNum.ToString());
        //tmpDisplay();

        gameRenderer.MyUpdate(entities, actionList);
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
