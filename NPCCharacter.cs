using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KevinIglesias;
using System;
using UnityEngine.AddressableAssets;

public class NPCCharacter : MonoBehaviour, iNPC
{
    private const string ES3_KEY = "_NPC_PLAYER_CLASS";

    private bool play;

    public float sphereOfAwareness = 20f;

    [SerializeField]
    private iCharacter.MagicType magicType;
    [SerializeField]
    private bool isMagicUser;
    [SerializeField]
    private bool hasRightHandWeapon;
    [SerializeField]
    private bool hasLeftHandShield;
    [SerializeField]
    private bool hasLargeWeapon;
    [SerializeField]
    private bool isArcher;

    [SerializeField]
    private AssetReferenceGameObject fireballCastFx;
    [SerializeField]
    private AssetReferenceGameObject poisonballCastFx;
    [SerializeField]
    private AssetReferenceGameObject frostballCastFx;
    [SerializeField]
    private AssetReferenceGameObject electricballCastFx;
    [SerializeField]
    private AssetReferenceGameObject fireballFx;
    [SerializeField]
    private AssetReferenceGameObject poisonballFx;
    [SerializeField]
    private AssetReferenceGameObject frostBallFx;
    [SerializeField]
    private AssetReferenceGameObject electricBallFx;

    private Animator animator;

    private int isRightHand;
    private int isLargeWeapon;
    private int isShieldInLeftHand;
    private int isHit;
    private int isHit1;
    private int isHit2;
    private int isHit3;
    private int isMagicUserHash;
    private int isArcherHash;

    private bool hitSwitch;
    private IKHelperTool ikTool;

    private iCharacter.CharacterState state;

    private iInventory inventory;

    //this is on the child node, model with the animator    
    private iCharacterCustomizer customize;

    private int attributePoints = 0;
    private int featPoints = 0;
    private int totalProficiency = 0;

    private string guid;
    private string characterName;

    private bool hasCharacterCreated;

    private iActionExecuter archeryActionExecutor;
    private iActionExecuter magicActionExecutor;
    private iActionExecuter meleeActionExecutor;
    private iActionExecuter magicBlockActionExecutor;
    private iActionExecuter physicalBlockActionExecutor;
    private iActionExecuter actionOne;
    private iActionExecuter actionTwo;

    private CharacterConfiguration.HostilityScore towardMales;
    private CharacterConfiguration.HostilityScore towardFemales;
    private CharacterConfiguration.HostilityScore towardFireforged;
    private CharacterConfiguration.HostilityScore towardStormborne;
    private CharacterConfiguration.HostilityScore towardFrostborne;
    private CharacterConfiguration.HostilityScore towardWoodbound;
    private CharacterConfiguration.HostilityScore towardFortworther;
    private CharacterConfiguration.HostilityScore towardPrimi;
    private CharacterConfiguration.HostilityScore towardStrider;
    private CharacterConfiguration.HostilityScore towardOtherNPCs;
    private CharacterConfiguration.HostilityScore towardUndead;
    private CharacterConfiguration.HostilityScore towardGoblins;

    private bool hasEngagedInDialogue;
    private bool isSetToFollow;

    private List<iCharacter> attackers = new List<iCharacter>();

    private NPCBehavior behavior;

    // Start is called before the first frame update
    private void Awake()
    {
        hasCharacterCreated = false;

        state = new iCharacter.CharacterState();
        state.SetAttribute(iCharacter.Attribute.STRENGTH, 0);
        state.SetAttribute(iCharacter.Attribute.DEXTERITY, 0);
        state.SetAttribute(iCharacter.Attribute.CONSTITUTION, 0);
        state.SetAttribute(iCharacter.Attribute.INTELLIGENCE, 0);
        state.SetAttribute(iCharacter.Attribute.WISDOM, 0);
        state.SetAttribute(iCharacter.Attribute.CHARISMA, 0);
        state.Fatigue = 0;
        state.Thirst = 0;
        state.Hunger = 0;
        state.Effects = new List<iEffect>();

        archeryActionExecutor = new ArcheryActionExector();
        magicActionExecutor = new NPCMagicActionExector(this,
                                                        fireballCastFx,
                                                        poisonballCastFx,
                                                        frostballCastFx,
                                                        electricballCastFx,
                                                        fireballFx,
                                                        poisonballFx,
                                                        frostBallFx,
                                                        electricBallFx);
        meleeActionExecutor = new NPCMeleeExector(this);
        magicBlockActionExecutor = new MagicBlockExector();
        physicalBlockActionExecutor = new NPCPhysicalBlockExector();

        SetIsUnarmed();

        state.Proficiencies.Add(new Archery(ProficiencyCatalog.Instance.proficiencies[ProficiencyCatalog.INDEX_ARCHERY]));
        state.Proficiencies.Add(new Axes(ProficiencyCatalog.Instance.proficiencies[ProficiencyCatalog.INDEX_AXES]));
        state.Proficiencies.Add(new Blades(ProficiencyCatalog.Instance.proficiencies[ProficiencyCatalog.INDEX_BLADE]));
        state.Proficiencies.Add(new Blunt(ProficiencyCatalog.Instance.proficiencies[ProficiencyCatalog.INDEX_BLUNT]));
        state.Proficiencies.Add(new Cooking(ProficiencyCatalog.Instance.proficiencies[ProficiencyCatalog.INDEX_COOKING]));
        state.Proficiencies.Add(new GreatWeapons(ProficiencyCatalog.Instance.proficiencies[ProficiencyCatalog.INDEX_GREAT_WEAPONS]));
        state.Proficiencies.Add(new MagicFire(ProficiencyCatalog.Instance.proficiencies[ProficiencyCatalog.INDEX_MAGIC_FIRE]));
        state.Proficiencies.Add(new MagicIce(ProficiencyCatalog.Instance.proficiencies[ProficiencyCatalog.INDEX_MAGIC_ICE]));
        state.Proficiencies.Add(new MagicPoison(ProficiencyCatalog.Instance.proficiencies[ProficiencyCatalog.INDEX_MAGIC_POISON]));
        state.Proficiencies.Add(new MagicStorm(ProficiencyCatalog.Instance.proficiencies[ProficiencyCatalog.INDEX_MAGIC_STORM]));
        state.Proficiencies.Add(new Medicine(ProficiencyCatalog.Instance.proficiencies[ProficiencyCatalog.INDEX_MEDICINE]));
        state.Proficiencies.Add(new Shield(ProficiencyCatalog.Instance.proficiencies[ProficiencyCatalog.INDEX_SHIELD]));
        state.Proficiencies.Add(new Survival(ProficiencyCatalog.Instance.proficiencies[ProficiencyCatalog.INDEX_SURVIVAL]));
        state.Proficiencies.Add(new Tinkering(ProficiencyCatalog.Instance.proficiencies[ProficiencyCatalog.INDEX_TINKERING]));
        state.Proficiencies.Add(new Unarmed(ProficiencyCatalog.Instance.proficiencies[ProficiencyCatalog.INDEX_UNARMED]));
        state.Proficiencies.Add(new LightArmor(ProficiencyCatalog.Instance.proficiencies[ProficiencyCatalog.INDEX_LIGHT_ARMOR]));
        state.Proficiencies.Add(new MediumArmor(ProficiencyCatalog.Instance.proficiencies[ProficiencyCatalog.INDEX_MEDIUM_ARMOR]));
        state.Proficiencies.Add(new HeavyArmor(ProficiencyCatalog.Instance.proficiencies[ProficiencyCatalog.INDEX_HEAVY_ARMOR]));
        state.Proficiencies.Add(new Bartering(ProficiencyCatalog.Instance.proficiencies[ProficiencyCatalog.INDEX_BARTERING]));

        customize = GetComponentInChildren<iCharacterCustomizer>();
        inventory = new Inventory(this, iInventory.LBS_PER_STRENGTH * 3);

        ikTool = GetComponentInChildren<IKHelperTool>();
        isHit = Animator.StringToHash("isHit");
        isHit1 = Animator.StringToHash("isHit1");
        isHit2 = Animator.StringToHash("isHit2");
        isHit3 = Animator.StringToHash("isHit3");
        isArcherHash = Animator.StringToHash("isArcher");
        isMagicUserHash = Animator.StringToHash("isMagicUser");
        isLargeWeapon = Animator.StringToHash("isLargeWeapon");
        isShieldInLeftHand = Animator.StringToHash("isShield");
        isRightHand = Animator.StringToHash("isRightHand");
        animator = GetComponentInChildren<Animator>();

        CharacterRegistry characterRegistry = FindObjectOfType<CharacterRegistry>();
        characterRegistry.Register(this);

        behavior = GetComponent<NPCBehavior>();

        StartCoroutine(MainLoop());
    }

    private void ApplyRace()
    {
        //TODO
    }

    private void ClearGender()
    {
        if (customize != null)
        {
            if (customize.IsMale())
            {
                state.SetAttribute(iCharacter.Attribute.STRENGTH, state.GetAttribute(iCharacter.Attribute.STRENGTH) - 1);
                state.SetAttribute(iCharacter.Attribute.CONSTITUTION, state.GetAttribute(iCharacter.Attribute.CONSTITUTION) - 1);
            }
            else
            {
                state.SetAttribute(iCharacter.Attribute.INTELLIGENCE, state.GetAttribute(iCharacter.Attribute.INTELLIGENCE) - 1);
                state.SetAttribute(iCharacter.Attribute.CHARISMA, state.GetAttribute(iCharacter.Attribute.CHARISMA) - 1);
            }
        }
    }

    private IEnumerator MainLoop()
    {
        play = true;

        while (play)
        {
            HandleIKTool();
            HandleAnimatorState();
            HandleActionExecutorState();
            HandleEffect();
            yield return null;
        }
    }

    private void HandleEffect()
    {
        state.Effects.ForEach(x => x.HandleInterval());
        state.Effects.RemoveAll(x => x.GetDuration() <= 0);
    }

    private void HandleActionExecutorState()
    {
        SetActionExecutionHandlers();
        if (actionOne != null)
            actionOne.ExecuteAction(GetRootTransform().gameObject, behavior.ActionOne);
        if (actionTwo != null)
            actionTwo.ExecuteAction(GetRootTransform().gameObject, behavior.ActionTwo);
    }

    private void SetActionExecutionHandlers()
    {
        if (inventory.GetEquippedSlot(Item.PaperDollSlot.MAIN_HAND) == null)
        {
            actionOne = meleeActionExecutor;
        }
        else if (inventory.GetEquippedSlot(Item.PaperDollSlot.MAIN_HAND) != null)
        {
            if (inventory.GetEquippedSlot(Item.PaperDollSlot.MAIN_HAND).GetWeaponType() == Item.WeaponType.BOW)
                actionOne = archeryActionExecutor;
            else if (magicType != iCharacter.MagicType.NONE)
                actionOne = magicActionExecutor;
            else
                actionOne = meleeActionExecutor;
        }

        if (inventory.GetEquippedSlot(Item.PaperDollSlot.OFF_HAND) == null)
        {
            if (inventory.GetEquippedSlot(Item.PaperDollSlot.MAIN_HAND) == null)
                actionTwo = physicalBlockActionExecutor;
            else if (inventory.GetEquippedSlot(Item.PaperDollSlot.MAIN_HAND) != null && magicType == iCharacter.MagicType.NONE &&
                inventory.GetEquippedSlot(Item.PaperDollSlot.MAIN_HAND).GetWeaponType() != Item.WeaponType.BOW)
                actionTwo = physicalBlockActionExecutor;
            else if (inventory.GetEquippedSlot(Item.PaperDollSlot.MAIN_HAND) != null && magicType != iCharacter.MagicType.NONE)
                actionTwo = magicBlockActionExecutor;
            else if (inventory.GetEquippedSlot(Item.PaperDollSlot.MAIN_HAND) != null &&
                    inventory.GetEquippedSlot(Item.PaperDollSlot.MAIN_HAND).GetWeaponType() == Item.WeaponType.BOW)
                actionTwo = null;
        }
        else
        {
            actionTwo = physicalBlockActionExecutor;
        }

    }

    private void HandleAnimatorState()
    {
        animator.SetBool(isArcherHash, isArcher);
        animator.SetBool(isLargeWeapon, hasLargeWeapon);
        animator.SetBool(isShieldInLeftHand, hasLeftHandShield);
        animator.SetBool(isRightHand, hasRightHandWeapon);
        animator.SetBool(isMagicUserHash, isMagicUser);
    }

    private void HandleIKTool()
    {
        if (hasLargeWeapon)
            ikTool.enabled = true;
        else
            ikTool.enabled = false;
    }

    public void Play()
    {
        play = true;
    }

    public bool IsRightHandWithWeapon()
    {
        return hasRightHandWeapon;
    }

    public void SetIsRightHandWithWeapon(bool value)
    {
        hasRightHandWeapon = value;
        if (value)
        {
            isMagicUser = false;
            hasLargeWeapon = false;
            isArcher = false;
        }
    }

    public bool IsActive()
    {
        return play;
    }

    public bool IsHoldingShield()
    {
        return hasLeftHandShield;
    }

    public void SetIsHoldingShield(bool value)
    {
        hasLeftHandShield = value;
        if (value)
        {
            //isMagicUser = false;
            hasLargeWeapon = false;
            isArcher = false;
            if (inventory.GetEquippedSlot(Item.PaperDollSlot.MAIN_HAND) != null &&
                (inventory.GetEquippedSlot(Item.PaperDollSlot.MAIN_HAND).GetWeaponType() == Item.WeaponType.GREAT_WEAPON ||
                inventory.GetEquippedSlot(Item.PaperDollSlot.MAIN_HAND).GetWeaponType() == Item.WeaponType.BOW))
                inventory.RemoveEquippedSlot(Item.PaperDollSlot.MAIN_HAND);
        }
    }

    public void TriggerHit()
    {
        animator.SetTrigger(isHit);
        if (hitSwitch)
        {
            animator.SetTrigger(isHit1);
        }
        else
        {
            animator.SetTrigger(isHit2);
        }

        LeanTween.delayedCall(0.5f, () =>
        {
            animator.ResetTrigger(isHit);
            animator.ResetTrigger(isHit1);
            animator.ResetTrigger(isHit2);
        });

        hitSwitch = !hitSwitch;
    }

    public void TriggerHeavyHit()
    {
        animator.SetTrigger(isHit);
        animator.SetTrigger(isHit3);
        LeanTween.delayedCall(0.5f, () =>
        {
            animator.ResetTrigger(isHit);
            animator.ResetTrigger(isHit3);
        });
    }

    public void SetIsHoldingLargeWeapon(bool value)
    {
        hasLargeWeapon = value;
        if (value)
        {
            isMagicUser = false;
            isArcher = false;
            hasRightHandWeapon = false;
            hasLeftHandShield = false;
            inventory.RemoveEquippedSlot(Item.PaperDollSlot.OFF_HAND);
        }
    }

    public bool IsHoldingLargeWeapon()
    {
        return hasLargeWeapon;
    }

    public bool IsMagicUser()
    {
        return isMagicUser;
    }

    public iCharacter.MagicType GetMagicType()
    {
        return magicType;
    }

    public bool IsArcher()
    {
        return isArcher;
    }

    public void SetIsArcher(bool value)
    {
        isArcher = value;
        if (value)
        {
            isMagicUser = false;
            hasRightHandWeapon = false;
            hasLeftHandShield = false;
            hasLargeWeapon = false;
        }
    }

    public void SetRace(Race race)
    {
        if (race != null)
        {
            state.Race = race.type;
            ApplyRace();
        }
    }

    public float GetAttackSpeed()
    {
        throw new System.NotImplementedException();
    }

    public void SetAttackSpeed(float value)
    {
        throw new System.NotImplementedException();
    }

    public float GetMovementSpeed()
    {
        throw new System.NotImplementedException();
    }

    public void SetMovementSpeed(float value)
    {
        throw new System.NotImplementedException();
    }

    public iCharacter.CharacterState GetCharacterState()
    {
        return state;
    }

    public void SetCharacterState(iCharacter.CharacterState state)
    {
        this.state = state;
    }

    public void SetInventory(iInventory inventory)
    {
        this.inventory = inventory;
    }

    public iInventory GetInventory()
    {
        return inventory;
    }

    public void SetCharacterCustomization(iCharacterCustomizer customizer)
    {
        ClearGender();
        customize = customizer;
    }

    public iCharacterCustomizer GetCharacterCustomizer()
    {
        return customize;
    }

    public Transform GetRootTransform()
    {
        return transform.root;
    }

    public void SetIsMagicUser(iCharacter.MagicType type)
    {
        if (type != iCharacter.MagicType.NONE)
        {
            isMagicUser = true;
            isArcher = false;
            //hasLeftHandShield = false;
            hasRightHandWeapon = false;
            hasLargeWeapon = false;
        }
        else
        {
            isMagicUser = false;
        }
        magicType = type;
        UpdateCastEffect();
    }

    private void UpdateCastEffect()
    {
        NPCMagicActionExector forMagicEffect = (NPCMagicActionExector)magicActionExecutor;
        //clean up
        forMagicEffect.CleanUp(GetRootTransform().gameObject);
        if (magicType != iCharacter.MagicType.NONE)
        {
            LeanTween.delayedCall(0.1f, () =>
            {
                iEquippable mainHand = inventory.GetEquippedSlot(Item.PaperDollSlot.MAIN_HAND);
                iEquippable offHand = inventory.GetEquippedSlot(Item.PaperDollSlot.OFF_HAND);
                if (offHand == null && mainHand != null && (mainHand.GetWeaponType() == Item.WeaponType.MAGIC_FOCUS_FIRE ||
                     mainHand.GetWeaponType() == Item.WeaponType.MAGIC_FOCUS_ICE ||
                     mainHand.GetWeaponType() == Item.WeaponType.MAGIC_FOCUS_POISON ||
                     mainHand.GetWeaponType() == Item.WeaponType.MAGIC_FOCUS_STORM))
                {
                    magicType = GetMagicType(mainHand);
                    forMagicEffect.InitiateHandEffect(this, GetRootTransform().gameObject, true);
                    forMagicEffect.InitiateHandEffect(this, GetRootTransform().gameObject, false);
                }
                else if (offHand != null && mainHand != null && offHand.GetWeaponType() == Item.WeaponType.SHIELD &&
                    (mainHand.GetWeaponType() == Item.WeaponType.MAGIC_FOCUS_FIRE ||
                     mainHand.GetWeaponType() == Item.WeaponType.MAGIC_FOCUS_ICE ||
                     mainHand.GetWeaponType() == Item.WeaponType.MAGIC_FOCUS_POISON ||
                     mainHand.GetWeaponType() == Item.WeaponType.MAGIC_FOCUS_STORM))
                {
                    magicType = GetMagicType(mainHand);
                    forMagicEffect.InitiateHandEffect(this, GetRootTransform().gameObject, true);
                }
            });
        }
    }

    private iCharacter.MagicType GetMagicType(iEquippable mainHand)
    {
        if (mainHand.GetWeaponType() == Item.WeaponType.MAGIC_FOCUS_FIRE)
            return iCharacter.MagicType.FIRE;
        else if (mainHand.GetWeaponType() == Item.WeaponType.MAGIC_FOCUS_ICE)
            return iCharacter.MagicType.FROST;
        else if (mainHand.GetWeaponType() == Item.WeaponType.MAGIC_FOCUS_POISON)
            return iCharacter.MagicType.POISON;
        else
            return iCharacter.MagicType.ELECTRIC;
    }

    public void SetIsUnarmed()
    {
        isMagicUser = false;
        hasLargeWeapon = false;
        isArcher = false;
        //hasLeftHandShield = false;
        hasRightHandWeapon = false;
    }

    public void SetAttributePoints(int value)
    {
        attributePoints = value;
    }

    public int GetAttributePoints()
    {
        return attributePoints;
    }

    public int GetFeatPoints()
    {
        return featPoints;
    }

    public void SetFeatPoints(int value)
    {
        featPoints = value;
    }

    public int GetTotalProficiency()
    {
        totalProficiency = 0;
        for (int i = 0; i < state.Proficiencies.Count; i++)
            totalProficiency += state.Proficiencies[i].GetLevel();
        return totalProficiency;
    }

    public void Save(string guid)
    {
        string json = ToJson();
        ES3.Save<string>(guid + ES3_KEY, json);
    }

    public bool Load(string guid)
    {
        if (ES3.KeyExists(guid + ES3_KEY))
        {
            string json = ES3.Load<string>(guid + ES3_KEY);
            FromJson(json);
            return true;
        }
        else
        {
            if (!hasCharacterCreated)
            {
                iCharacterBuilder builder = transform.root.GetComponentInChildren<iCharacterBuilder>();
                builder.Create();
                hasCharacterCreated = true;
            }
        }
        return false;
    }

    public void FromJson(string json)
    {
        AsJson asJson = JsonUtility.FromJson<AsJson>(json);
        //will need to remove old guid from registry and register saved one
        CharacterRegistry registry = FindObjectOfType<CharacterRegistry>();
        registry.RemoveCharacter(guid);
        guid = asJson.guid;
        registry.Register(this);
        attributePoints = asJson.attributePoints;
        featPoints = asJson.featPoints;
        totalProficiency = asJson.totalProficiency;
        state.FromJson(asJson.characterState);
        customize.FromJson(asJson.customize);
        inventory.FromJson(asJson.inventory);
        hasCharacterCreated = asJson.hasCharacterCreated;
        isArcher = asJson.isArcher;
        hasLargeWeapon = asJson.hasLargeWeapon;
        hasLeftHandShield = asJson.hasLeftHandShield;
        hasRightHandWeapon = asJson.hasRightHandWeapon;
        isMagicUser = asJson.isMagicUser;

        towardMales = GetHostilityScore(asJson.towardMales);
        towardFemales = GetHostilityScore(asJson.towardFemales);
        towardFireforged = GetHostilityScore(asJson.towardFireforged);
        towardStormborne = GetHostilityScore(asJson.towardStormborne);
        towardFrostborne = GetHostilityScore(asJson.towardFrostborne);
        towardWoodbound = GetHostilityScore(asJson.towardWoodbound);
        towardFortworther = GetHostilityScore(asJson.towardFortworther);
        towardPrimi = GetHostilityScore(asJson.towardPrimi);
        towardStrider = GetHostilityScore(asJson.towardStrider);
        towardOtherNPCs = GetHostilityScore(asJson.towardOtherNPCs);
        towardUndead = GetHostilityScore(asJson.towardUndead);
        towardGoblins = GetHostilityScore(asJson.towardGoblins);

        attackers = FromArray(asJson.isInCombat);
        hasEngagedInDialogue = asJson.hasEngagedInDialogue;

        if (hasEngagedInDialogue)
            Destroy(GetComponent<DialogueUIController>());
        else
            GetComponent<DialogueUIController>().FromJson(asJson.dialogueController);

        magicType = FindMagicType(asJson.magicType);
        GetRootTransform().position = new Vector3(asJson.positionX, asJson.positionY, asJson.positionZ);
        GetRootTransform().eulerAngles = new Vector3(asJson.eulerX, asJson.eulerY, asJson.eulerZ);

        isSetToFollow = asJson.isSetToFollow;

        if (!isArcher && !isMagicUser && !hasLargeWeapon && !hasRightHandWeapon)
            SetIsUnarmed();

        if (hasLeftHandShield)
            SetIsHoldingShield(hasLeftHandShield);

    }

    private CharacterConfiguration.HostilityScore GetHostilityScore(int id)
    {
        return (CharacterConfiguration.HostilityScore)Enum.ToObject(typeof(CharacterConfiguration.HostilityScore), id);
    }

    private iCharacter.MagicType FindMagicType(int magicId)
    {
        foreach (iCharacter.MagicType magicType in Enum.GetValues(typeof(iCharacter.MagicType)))
            if ((int)magicType == magicId)
                return magicType;
        return iCharacter.MagicType.NONE;
    }

    public string ToJson()
    {
        AsJson asJson = new AsJson();
        asJson.guid = guid;
        asJson.attributePoints = attributePoints;
        asJson.featPoints = featPoints;
        asJson.totalProficiency = totalProficiency;
        asJson.characterState = state.ToJson();
        asJson.inventory = inventory.ToJson();
        asJson.customize = customize.ToJson();
        asJson.hasCharacterCreated = hasCharacterCreated;
        asJson.isArcher = isArcher;
        asJson.hasLargeWeapon = hasLargeWeapon;
        asJson.hasLeftHandShield = hasLeftHandShield;
        asJson.hasRightHandWeapon = hasRightHandWeapon;
        asJson.isMagicUser = isMagicUser;
        asJson.magicType = (int)magicType;
        asJson.positionX = GetRootTransform().position.x;
        asJson.positionY = GetRootTransform().position.y;
        asJson.positionZ = GetRootTransform().position.z;
        asJson.eulerX = GetRootTransform().eulerAngles.x;
        asJson.eulerY = GetRootTransform().eulerAngles.y;
        asJson.eulerZ = GetRootTransform().eulerAngles.z;

        asJson.towardMales = (int)towardMales;
        asJson.towardFemales = (int)towardFemales;
        asJson.towardFireforged = (int)towardFireforged;
        asJson.towardStormborne = (int)towardStormborne;
        asJson.towardFrostborne = (int)towardFrostborne;
        asJson.towardWoodbound = (int)towardWoodbound;
        asJson.towardFortworther = (int)towardFortworther;
        asJson.towardPrimi = (int)towardPrimi;
        asJson.towardStrider = (int)towardStrider;
        asJson.towardOtherNPCs = (int)towardOtherNPCs;
        asJson.towardUndead = (int)towardUndead;
        asJson.towardGoblins = (int)towardGoblins;

        asJson.hasEngagedInDialogue = hasEngagedInDialogue;
        asJson.isInCombat = ToArray(attackers);
        asJson.isSetToFollow = isSetToFollow;

        if (GetComponent<DialogueUIController>() != null)
            asJson.dialogueController = GetComponent<DialogueUIController>().ToJson();

        return JsonUtility.ToJson(asJson);
    }

    private string[] ToArray(List<iCharacter> list)
    {
        List<string> ids = new List<string>();
        foreach (iCharacter character in list)
        {
            ids.Add(character.GetId());
        }
        return ids.ToArray();
    }

    private List<iCharacter> FromArray(string[] ids)
    {
        List<iCharacter> attackers = new List<iCharacter>();
        CharacterRegistry registry = FindObjectOfType<CharacterRegistry>();
        foreach (string id in ids)
        {
            attackers.Add(registry.GetCharacter(id));
        }

        return attackers;
    }

    public string GetId()
    {
        if (guid == null)
        {
            guid = Guid.NewGuid().ToString();
        }
        return guid;
    }

    public bool HasCreatedCharacter()
    {
        return hasCharacterCreated;
    }

    public void SetHasCreatedCharacter(bool value)
    {
        hasCharacterCreated = value;
    }

    public void SetCharacterName(string value)
    {
        characterName = value;
    }

    public string GetCharacterName()
    {
        return characterName;
    }

    public void SetSphereOfAwareness(float radius)
    {
        sphereOfAwareness = radius;
    }

    public float GetSphereOfAwareness()
    {
        return sphereOfAwareness;
    }

    public void SetCharacterConfiguration(CharacterConfiguration configuration)
    {
        towardMales = configuration.towardMales;
        towardFemales = configuration.towardFemales;
        towardFireforged = configuration.towardFireforged;
        towardStormborne = configuration.towardStormborne;
        towardFrostborne = configuration.towardFrostborne;
        towardWoodbound = configuration.towardWoodbound;
        towardFortworther = configuration.towardFortworther;
        towardPrimi = configuration.towardPrimi;
        towardStrider = configuration.towardStrider;
        towardOtherNPCs = configuration.towardOtherNPCs;
        towardUndead = configuration.towardUndead;
        towardGoblins = configuration.towardGoblins;
    }

    public bool IsInCombat()
    {
        return attackers.Count > 0;
    }

    public void SetIsInCombat(iCharacter attacker)
    {
        if (!attackers.Contains(attacker))
        {
            attackers.Add(attacker);
        }
    }

    public bool HasEngagedInDialogue()
    {
        return hasEngagedInDialogue;
    }

    public void SetHasEngagedInDialogue(bool value)
    {
        hasEngagedInDialogue = value;
    }

    public void SetToFollow(bool value)
    {
        isSetToFollow = value;
    }

    public bool IsSetToFollow()
    {
        return isSetToFollow;
    }

    public List<iCharacter> GetAttackers()
    {
        return attackers;
    }

    public iActionExecuter GetActionOne()
    {
        return actionOne;
    }

    private class AsJson
    {
        public string guid;
        public int attributePoints;
        public int featPoints;
        public int totalProficiency;
        public string characterState;
        public string inventory;
        public string customize;
        public bool hasCharacterCreated;
        public bool isArcher;
        public bool hasLargeWeapon;
        public bool hasLeftHandShield;
        public bool hasRightHandWeapon;
        public bool isMagicUser;
        public int magicType;
        public float positionX;
        public float positionY;
        public float positionZ;
        public float eulerX;
        public float eulerY;
        public float eulerZ;

        public int towardMales;
        public int towardFemales;
        public int towardFireforged;
        public int towardStormborne;
        public int towardFrostborne;
        public int towardWoodbound;
        public int towardFortworther;
        public int towardPrimi;
        public int towardStrider;
        public int towardOtherNPCs;
        public int towardUndead;
        public int towardGoblins;

        public string[] isInCombat;
        public bool hasEngagedInDialogue;
        public bool isSetToFollow;

        public string dialogueController;
    }

    private class ArcheryActionExector : iActionExecuter
    {
        public void ExecuteAction(GameObject sourceObject, bool toggle)
        {
            //TODO
        }
    }

    private class MagicBlockExector : iActionExecuter
    {
        public void ExecuteAction(GameObject sourceObject, bool toggle)
        {
            //TODO
        }
    }

}
