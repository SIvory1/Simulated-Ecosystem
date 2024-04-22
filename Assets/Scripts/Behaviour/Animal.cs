using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Animal : LivingEntity {

    public const int maxViewDistance = 10;

    [EnumFlags]
    public Species diet;

    public CreatureAction currentAction;
    public Genes genes;
    public Color maleColour;
    public Color femaleColour;

    // Settings:
    float timeBetweenActionChoices = 1;
    public float moveSpeed = 1.5f;
    float timeToDeathByHunger = 200;
    float timeToDeathByThirst = 200;
    float hungerMultiplier = 1;
    float minBreedChance = 0.5f;
    List<Animal> unimpressedFemales = new List<Animal>();

    float drinkDuration = 6;
    float eatDuration = 10;

    float criticalPercent = 0.7f;

    // Visual settings:
    float moveArcHeight = .2f;

    // State:
    [Header ("State")]
    public float hunger;
    public float thirst;
    public float reproductiveUrge;
    public float currentGestationDuration = 0.0f;
    public float maxGestationDuration = 15.0f;
    public bool pregnant = false;
    float mateWaitTimer = 0.0f;
    public float mateWaitTimerMax = 10.0f;
    float maturityTimer = 0.0f;
    public float maturityTimerMax = 15.0f;
    bool mature = false;

    public LivingEntity rabbitPrefab;

    protected LivingEntity foodTarget;
    protected Coord waterTarget;
    protected Animal mateTarget;

    // Move data:
    bool animatingMovement;
    Coord moveFromCoord;
    Coord moveTargetCoord;
    Vector3 moveStartPos;
    Vector3 moveTargetPos;
    float moveTime;
    float moveSpeedFactor;
    float moveArcHeightFactor;
    Coord[] path;
    int pathIndex;

    // Other
    float lastActionChooseTime;
    const float sqrtTwo = 1.4142f;
    const float oneOverSqrtTwo = 1 / sqrtTwo;

    // start of the worst flee you have ever seen
    public bool beingHunted = false;
    [SerializeField] Animal otherAnimal;
    public Animal animalToFleeFrom;
    

    public override void Init (Coord coord) {
        base.Init (coord);
        moveFromCoord = coord;
        genes = Genes.RandomGenes(4);
   
        material.color = (genes.isMale) ? maleColour : femaleColour;
        
        HappinessModification();
       
        ChooseNextAction ();
    }
    public override void Init(Coord coord, float[] mother, float[] father)
    {
        base.Init(coord);
        moveFromCoord = coord;
        genes = Genes.InheritedGenes(mother, father);

        material.color = (genes.isMale) ? maleColour : femaleColour;
        //print("DesirabilityMOTHER : " + mother.desirability + "DesirabilityFATHER : " + father.desirability + "Desirability : " + genes.desirability);
        
        HappinessModification();
        
        ChooseNextAction();
    }

    void HappinessModification()
    {
        float happinessMod = genes.happiness - 0.5f;
        moveSpeed += happinessMod;
        hungerMultiplier += happinessMod;
    }

    protected virtual void Update () {

        // Increase hunger and thirst over time
        hunger += (Time.deltaTime * hungerMultiplier) / timeToDeathByHunger;
        thirst += (Time.deltaTime * 1) / timeToDeathByThirst;

        if (pregnant) { reproductiveUrge = 0.0f; }
        else { reproductiveUrge = 0.4f; }

        if(pregnant) { currentGestationDuration += Time.deltaTime; }
        if(currentGestationDuration > maxGestationDuration) 
        { 
            pregnant = false;
            currentGestationDuration = 0f;
            print("babymake");
            Environment.SpawnChildren(rabbitPrefab, coord, genes.values, mateTarget.genes.values);
            //print(mateTarget.genes.values[1] + " " + genes.values[1]);
        }
        if(currentAction == CreatureAction.GoingToMate) { mateWaitTimer += Time.deltaTime; }
        if(currentAction != CreatureAction.GoingToMate) { mateWaitTimer = 0f; }
        if(mateWaitTimer > mateWaitTimerMax) {  mateWaitTimer = 0f; currentAction = CreatureAction.Exploring; }

        if (!mature) { maturityTimer += Time.deltaTime; }
        if(maturityTimer > maturityTimerMax) { mature = true; }

        // Animate movement. After moving a single tile, the animal will be able to choose its next action
        if (animatingMovement) {
            AnimateMove ();
        } else {
            // Handle interactions with external things, like food, water, mates
            HandleInteractions ();
            float timeSinceLastActionChoice = Time.time - lastActionChooseTime;
            if (timeSinceLastActionChoice > timeBetweenActionChoices) {
                ChooseNextAction ();
            }
        }

        if (hunger >= 1) {
            Die (CauseOfDeath.Hunger);
        } else if (thirst >= 1) {
            Die (CauseOfDeath.Thirst);
        }
    }

    // Animals choose their next action after each movement step (1 tile),
    // or, when not moving (e.g interacting with food etc), at a fixed time interval
    protected virtual void ChooseNextAction () {
        lastActionChooseTime = Time.time;
        // Get info about surroundings

        // Decide next action:
        // Eat if (more hungry than thirsty) or (currently eating and not critically thirsty)
        bool currentlyEating = currentAction == CreatureAction.Eating && foodTarget && hunger > 0;
        bool currentlyDrinking = currentAction == CreatureAction.Drinking && thirst > 0;

        FindPredator();
        if (beingHunted)
        {
            //currentAction = CreatureAction.Fleeing;
        }
        else if(reproductiveUrge > hunger && reproductiveUrge > thirst && !currentlyEating && !currentlyDrinking && mature)
        {
            FindMate();
        }
        else
        {
            if (hunger >= thirst || currentlyEating && thirst < criticalPercent)
            {
                FindFood();
            }
            // More thirsty than hungry
            else
            {
                FindWater();
            }
        }

        Act();
    }

    protected virtual void FindMate()
    {
        ResetOtherAnimal();
        if (currentAction != CreatureAction.GoingToMate)
        { 
            currentAction = CreatureAction.SearchingForMate;
        }
        if (genes.isMale) 
        {
            List<Animal> potentialFemales = Environment.SensePotentialMates(coord, this);

            for(int i = 0; i < potentialFemales.Count; i++)
            {
                if (!unimpressedFemales.Contains(potentialFemales[i]))
                {
                    if (potentialFemales[i].RequestMate(this))
                    {
                        mateTarget = potentialFemales[i];
                        currentAction = CreatureAction.GoingToMate;
                        CreatePath(potentialFemales[i].coord);
                        break;
                    }
                    else
                    {
                        unimpressedFemales.Add(potentialFemales[i]);
                    }
                }
            }
        } 
    }

    public bool RequestMate(Animal male)
    {
        float chance = Mathf.Lerp(minBreedChance, 1, male.genes.desirability);
        if(Random.value > chance)
        {
            return false;
        }
        mateTarget = male;
        currentAction = CreatureAction.GoingToMate;
        return true;
    }

    protected virtual void FindFood () {
        LivingEntity foodSource = Environment.SenseFood (coord, this, FoodPreferencePenalty);
        if (foodSource) {
            currentAction = CreatureAction.GoingToFood;
            foodTarget = foodSource;

            // Remove previous animal from being hunted
            //ResetOtherAnimal();
            // Add the new animal to being hunted
            //otherAnimal = foodTarget.GetComponent<Animal>();
            //if (otherAnimal != null)
            //{
            //    otherAnimal.beingHunted = true;
            //    otherAnimal.animalToFleeFrom = this;
            //}

            CreatePath (foodTarget.coord);

        } 
        else {
            currentAction = CreatureAction.Exploring;

            //ResetOtherAnimal();
        }
    }
    void ResetOtherAnimal()
    {
        if (otherAnimal != null)
        {
            otherAnimal.beingHunted = false;
            otherAnimal.animalToFleeFrom = null;
            otherAnimal = null;
        }
    }
    protected virtual void FindPredator()
    {
        LivingEntity predator = Environment.SensePredator(coord, this);
        if (predator)
        {
            animalToFleeFrom = predator.GetComponent<Animal>();
            beingHunted = true;
            currentAction = CreatureAction.Fleeing;
        }
        else
        {
            animalToFleeFrom = null;
            beingHunted = false;
            currentAction = CreatureAction.Exploring;
        }
    }

    protected virtual void FindWater () {
        Coord waterTile = Environment.SenseWater (coord);
        if (waterTile != Coord.invalid) {
            currentAction = CreatureAction.GoingToWater;
            waterTarget = waterTile;
            CreatePath (waterTarget);

        } else {
            currentAction = CreatureAction.Exploring;
        }
    }

    // When choosing from multiple food sources, the one with the lowest penalty will be selected
    protected virtual int FoodPreferencePenalty (LivingEntity self, LivingEntity food) {
        return Coord.SqrDistance (self.coord, food.coord);
    }

    protected void Act () {
        switch (currentAction) {
            case CreatureAction.Exploring:
                StartMoveToCoord (Environment.GetNextTileWeighted (coord, moveFromCoord));
                break;
            case CreatureAction.GoingToFood:
                if (Coord.AreNeighbours (coord, foodTarget.coord)) {
                    LookAt (foodTarget.coord);
                    currentAction = CreatureAction.Eating;
                } else {
                    StartMoveToCoord (path[pathIndex]);
                    pathIndex++;
                }
                break;
            case CreatureAction.GoingToWater:
                if (Coord.AreNeighbours (coord, waterTarget)) {
                    LookAt (waterTarget);
                    currentAction = CreatureAction.Drinking;
                } else {
                    StartMoveToCoord (path[pathIndex]);
                    pathIndex++;
                }
                break;
            case CreatureAction.SearchingForMate:
                StartMoveToCoord(Environment.GetNextTileWeighted(coord, moveFromCoord));
                break;
            case CreatureAction.GoingToMate:
                if (Coord.AreNeighbours(coord, mateTarget.coord))
                {
                    LookAt(mateTarget.coord);
                    currentAction = CreatureAction.Mating;
                }
                else
                {
                    if (genes.isMale)
                    {
                        StartMoveToCoord(path[pathIndex]);
                        pathIndex++;
                    }                    
                }
                break;
            case CreatureAction.Fleeing:
                StartMoveToCoord(Environment.FleeGetNextTileWeighted(coord, animalToFleeFrom.coord));
                break;
        }
    }

    protected void CreatePath (Coord target) {
        // Create new path if current is not already going to target
        if (path == null || pathIndex >= path.Length || (path[path.Length - 1] != target || path[pathIndex - 1] != moveTargetCoord)) {
            path = EnvironmentUtility.GetPath (coord.x, coord.y, target.x, target.y);
            pathIndex = 0;
        }
    }

    protected void StartMoveToCoord (Coord target) {
        moveFromCoord = coord;
        moveTargetCoord = target;
        moveStartPos = transform.position;
        moveTargetPos = Environment.tileCentres[moveTargetCoord.x, moveTargetCoord.y];
        animatingMovement = true;

        bool diagonalMove = Coord.SqrDistance (moveFromCoord, moveTargetCoord) > 1;
        moveArcHeightFactor = (diagonalMove) ? sqrtTwo : 1;
        moveSpeedFactor = (diagonalMove) ? oneOverSqrtTwo : 1;

        LookAt (moveTargetCoord);
    }

    protected void LookAt (Coord target) {
        if (target != coord) {
            Coord offset = target - coord;
            transform.eulerAngles = Vector3.up * Mathf.Atan2 (offset.x, offset.y) * Mathf.Rad2Deg;
        }
    }

    void HandleInteractions () {
        if (currentAction == CreatureAction.Eating) {
            if (foodTarget && hunger > 0) {
                float eatAmount = Mathf.Min (hunger, Time.deltaTime * 1 / eatDuration);
                eatAmount = foodTarget.Consume(eatAmount);
                hunger -= eatAmount;
            }
        } else if (currentAction == CreatureAction.Drinking) {
            if (thirst > 0) {
                thirst -= Time.deltaTime * 1 / drinkDuration;
                thirst = Mathf.Clamp01 (thirst);
            }
        }else if(currentAction == CreatureAction.Mating) 
        {
            if (!genes.isMale) { pregnant = true; }
            currentAction = CreatureAction.Exploring;
        }
    }

    void AnimateMove () {
        // Move in an arc from start to end tile
        moveTime = Mathf.Min (1, moveTime + Time.deltaTime * moveSpeed * moveSpeedFactor);
        float height = (1 - 4 * (moveTime - .5f) * (moveTime - .5f)) * moveArcHeight * moveArcHeightFactor;
        transform.position = Vector3.Lerp (moveStartPos, moveTargetPos, moveTime) + Vector3.up * height;

        // Finished moving
        if (moveTime >= 1) {
            Environment.RegisterMove(this, moveFromCoord, moveTargetCoord);
            coord = moveTargetCoord;

            animatingMovement = false;
            moveTime = 0;
            ChooseNextAction ();
        }
    }

    void OnDrawGizmosSelected () {
        if (Application.isPlaying) {
            var surroundings = Environment.Sense (coord);
            Gizmos.color = Color.white;
            if (surroundings.nearestFoodSource != null) {
                Gizmos.DrawLine (transform.position, surroundings.nearestFoodSource.transform.position);
            }
            if (surroundings.nearestWaterTile != Coord.invalid) {
                Gizmos.DrawLine (transform.position, Environment.tileCentres[surroundings.nearestWaterTile.x, surroundings.nearestWaterTile.y]);
            }

            if (surroundings.nearestThreat != null && species == Species.Rabbit)
            {
                Gizmos.DrawLine (transform.position, surroundings.nearestThreat.transform.position);
            }

            if (currentAction == CreatureAction.GoingToFood) {
                var path = EnvironmentUtility.GetPath (coord.x, coord.y, foodTarget.coord.x, foodTarget.coord.y);
                Gizmos.color = Color.black;
                for (int i = 0; i < path.Length; i++) {
                    Gizmos.DrawSphere (Environment.tileCentres[path[i].x, path[i].y], .2f);
                }
            }
        }
    }

}