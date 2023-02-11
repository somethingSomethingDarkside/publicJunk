using UnityEngine.AddressableAssets;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using TMPro;
using System;
using UnityEngine.UI;
using Random = UnityEngine.Random;

//TODO handle set to follow of the NPC
//TODO this should have a tojson and fromjson implementation
public class NPCBehavior : MonoBehaviour
{
    private const int LAYER_UNARMED = 2;
    private const int LAYER_1H_COMBAT = 3;
    private const int LAYER_2H_COMBAT = 4;
    private const float NORMAL_ACCELERATION = 6f;
    private const float SPRINT_ACCELERATION = 9f;
    private const float NORMAL_SPEED = 4f;
    private const float SPRINT_SPEED = 6f;
    private const float IDEAL_MELEE_RANGE = 1.25f;
    private const float IDEAL_RANGE_RANGE = 10f;
    private const float POPUP_TIME = 10f;
    private const float MIN_DIALOGUE_WAIT_TIME = 30f;
    private const float MAX_DIALOGUE_WAIT_TIME = 120f;
    private const float SLEEP_TIMER = 0.25f;
    private const float EVENT_DIALOGUE_WAIT_TIME = MIN_DIALOGUE_WAIT_TIME;
    private const float BASE_FORGET_DURATION = 15f;
    private const float LEASH_LENGTH = 50f;
    private const float SPELL_CAST_INTERVAL = 3f;

    public enum Emotion
    {
        HAPPY = 0,
        SAD = 1,
        HURT = 2,
        ANGRY = 3,
        DEAD = 4
    }

    private Image emoji;

    [SerializeField]
    private Transform lookTarget;

    [SerializeField]
    private AssetReferenceSprite sadSprite;
    [SerializeField]
    private AssetReferenceSprite happySprite;
    [SerializeField]
    private AssetReferenceSprite angrySprite;
    [SerializeField]
    private AssetReferenceSprite hurtSprite;
    [SerializeField]
    private AssetReferenceSprite deadSprite;

    [SerializeField]
    private GameObject popupText;
    [SerializeField]
    private LayerMask lineOfSightMask;

    [SerializeField]
    private LayerMask lookForMask;
    private LayerMask floorMask;

    private float xBound = 10f;
    private float yBound = 10f;
    private float zBound = 10f;

    private iNPC character;

    private float randomDialogueCountdown;
    private float eventDialogueCountdown;
    private float sleepTimer;

    private NavMeshAgent agent;
    private NavMeshHit navMeshHit;

    private List<ForgettableTarget> forgetMes = new List<ForgettableTarget>();
    private int[] layerOrder;

    private int layerPlayer;
    private int layerEnemy;
    private int layerNPC;

    //TODO will have emoji if someone is diseased will override general and discovery
    private Transform lastLookAtTarget;

    private Collider myCollider;

    private LTDescr descrInteractingWithDevice;
    private Vector3 deviceStartingPosition;
    private float deviceStartingHealth;
    private iCombatDevice device;

    private iCharacter player;

    private Vector3 positionWhenCombatStarted;
    private iCharacter combatTarget;
    private Animator combatAnimator;

    private Animator animator;

    private int animIDSprint;

    private int animIDUnarmedAttack1;
    private int animIDUnarmedAttack2;
    private int animID1HAttack1;
    private int animID1HAttack2;
    private int animID1HAttack3;
    private int animID1HAttack4;
    private int animID1HReverseAttack3;
    private int animID1HReverseAttack4;
    private int animID2HAttack1;
    private int animID2HAttack2;
    private int animID2HAttack3;
    private int animID2HAttack4;
    private int animID2HReverseAttack3;
    private int animID2HReverseAttack4;

    public bool ActionOne { get; set; }
    public bool ActionTwo { get; set; }

    private LayerMask projectileMask;

    private void Start()
    {
        player = FindObjectOfType<Player>();
        character = transform.root.GetComponentInChildren<iNPC>();
        randomDialogueCountdown = UnityEngine.Random.Range(MIN_DIALOGUE_WAIT_TIME, MAX_DIALOGUE_WAIT_TIME);

        projectileMask = LayerMask.GetMask(new string[] { "Arrow" });

        character.GetAttackers().Clear();

        myCollider = character.GetRootTransform().GetComponent<Collider>();
        agent = character.GetRootTransform().GetComponent<NavMeshAgent>();
        NavMesh.avoidancePredictionTime = 0.25f;

        layerPlayer = LayerMask.NameToLayer("Player");
        layerEnemy = LayerMask.NameToLayer("Enemy");
        layerNPC = LayerMask.NameToLayer("NPC");

        animator = GetComponentInChildren<Animator>();

        animIDSprint = Animator.StringToHash("isSprinting");
        animIDUnarmedAttack1 = Animator.StringToHash("Attack1");
        animIDUnarmedAttack2 = Animator.StringToHash("Attack2");
        animID1HAttack1 = Animator.StringToHash("Attack1");
        animID1HAttack2 = Animator.StringToHash("Attack2");
        animID1HAttack3 = Animator.StringToHash("Attack3");
        animID1HReverseAttack3 = Animator.StringToHash("Attack3_Reverse");
        animID1HAttack4 = Animator.StringToHash("Attack4");
        animID1HReverseAttack4 = Animator.StringToHash("Attack4_Reverse");
        animID2HAttack1 = Animator.StringToHash("Attack1");
        animID2HAttack2 = Animator.StringToHash("Attack2");
        animID2HAttack3 = Animator.StringToHash("Attack3");
        animID2HAttack4 = Animator.StringToHash("Attack4");
        animID2HReverseAttack3 = Animator.StringToHash("Attack3_Reverse");
        animID2HReverseAttack4 = Animator.StringToHash("Attack4_Reverse");

        //Player Enemy Interactable Item Arrow NPC
        layerOrder = new int[]
            {
            layerPlayer,
            layerEnemy,
            LayerMask.NameToLayer("Item"),
            LayerMask.NameToLayer("Interactable"),
            layerNPC,
            LayerMask.NameToLayer("Arrow")
            };

        floorMask = LayerMask.GetMask(new string[] { "Floor" });

        StartCoroutine(MainLoop());
    }

    private IEnumerator MainLoop()
    {
        while (character.IsActive())
        {
            if (agent != null && agent.enabled)
            {
                HandleDialogue();
                HandleForgetMe();
                HandleLookingAround();
                HandleDevice();
                HandleCombat();
            }
            yield return null;
        }
    }

    private void HandleCombat()
    {
        if (!character.IsInCombat())
        {
            positionWhenCombatStarted = transform.position;
        }

        if (character.IsInCombat() && Vector3.Distance(positionWhenCombatStarted, transform.position) <= LEASH_LENGTH)
        {
            //PopDialogue(DialogueCatalog.Find(character.GetCharacterState().Race).discoveringAnEnemy, angrySprite)
            //find source of attack
            if (combatTarget == null)
                combatTarget = GetTarget(character.GetAttackers());

            if (combatTarget != null)
            {
                LookAt(combatTarget.GetRootTransform());

                //determine if this character is range or melee - if range, then try to maintain range
                bool isMelee = IsMelee(character.GetInventory().GetEquippedSlot(Item.PaperDollSlot.MAIN_HAND));
                if (isMelee)
                {
                    HandleMelee();
                }
                else
                {
                    HandleRange();
                }
            }
        }
        else if (character.IsInCombat() && Vector3.Distance(positionWhenCombatStarted, transform.position) > LEASH_LENGTH)
        {
            character.GetAttackers().Remove(combatTarget);
            combatTarget = null;
            combatAnimator = null;
            agent.acceleration = NORMAL_ACCELERATION;
            agent.speed = NORMAL_SPEED;
            ActionOne = false;
            ActionTwo = false;
        }

    }

    private void HandleRange()
    {
        //range
        if (Vector3.Distance(transform.position, combatTarget.GetRootTransform().position) > IDEAL_RANGE_RANGE ||
            Vector3.Distance(transform.position, combatTarget.GetRootTransform().position) <= IDEAL_MELEE_RANGE)
            agent.SetDestination(GetDestinationAtDesiredDistance(combatTarget.GetRootTransform(), IDEAL_RANGE_RANGE));
        else
        {
            //rotate toward target and attack
            LeanTween.rotateY(character.GetRootTransform().gameObject, GetYRotation(combatTarget.GetRootTransform()), 0.1f);
            ActionOne = true;
            LeanTween.delayedCall(0.1f, () =>
            {
                if (character.GetActionOne() != null && character.GetActionOne() is NPCMagicActionExector)
                {
                    NPCMagicActionExector executor = (NPCMagicActionExector)character.GetActionOne();
                    executor.SetInterval(SPELL_CAST_INTERVAL);
                }
            });
        }

        //TODO sense incoming arrows (projectile) and block
    }

    //TODO need to add code to force attack if done nothing but blocked x amount of attacks
    private void HandleMelee()
    {
        if (Vector3.Distance(combatTarget.GetRootTransform().position, transform.position) > IDEAL_RANGE_RANGE)
        {
            agent.acceleration = SPRINT_ACCELERATION;
            agent.speed = SPRINT_SPEED;
            animator.SetBool(animIDSprint, true);
        }
        else
        {
            agent.acceleration = NORMAL_ACCELERATION;
            agent.speed = NORMAL_SPEED;
            animator.SetBool(animIDSprint, false);
        }

        //melee 
        if (Vector3.Distance(transform.position, combatTarget.GetRootTransform().position) > IDEAL_MELEE_RANGE)
            agent.SetDestination(GetDestinationAtDesiredDistance(combatTarget.GetRootTransform(), IDEAL_MELEE_RANGE));
        else // if (!LeanTween.isTweening(character.GetRootTransform().gameObject))
        {
            LeanTween.rotateY(character.GetRootTransform().gameObject, GetYRotation(combatTarget.GetRootTransform()), 0.1f);
            if (Vector3.Distance(transform.position, combatTarget.GetRootTransform().position) <= IDEAL_MELEE_RANGE)
            {
                ActionOne = true;
            }
        }

        if (combatAnimator == null)
            combatAnimator = combatTarget.GetRootTransform().GetComponentInChildren<Animator>();

        //TODO sense incoming projectile and block it
        if (IsTargetAttacking(combatAnimator) && !ActionOne)
        {
            ActionTwo = true;
            ActionOne = false;
        }
        else
        {
            ActionTwo = false;
        }
    }

    public float GetYRotation(Transform target)
    {
        Vector3 targetDirection = (target.position - character.GetRootTransform().position).normalized;
        float yRotation = Mathf.Atan2(targetDirection.x, targetDirection.z) * Mathf.Rad2Deg - character.GetRootTransform().eulerAngles.y;

        if (yRotation > 180f)
        {
            yRotation -= 360f;
        }
        else if (yRotation < -180f)
        {
            yRotation += 360f;
        }

        return yRotation;
    }

    public bool IsTargetAttacking(Animator targetAnimator)
    {
        if (targetAnimator != null)
        {
            if (targetAnimator.GetCurrentAnimatorStateInfo(LAYER_UNARMED).shortNameHash == animIDUnarmedAttack1 ||
                targetAnimator.GetCurrentAnimatorStateInfo(LAYER_1H_COMBAT).shortNameHash == animIDUnarmedAttack2 ||
                targetAnimator.GetCurrentAnimatorStateInfo(LAYER_1H_COMBAT).shortNameHash == animID1HAttack1 ||
                targetAnimator.GetCurrentAnimatorStateInfo(LAYER_1H_COMBAT).shortNameHash == animID1HAttack2 ||
                targetAnimator.GetCurrentAnimatorStateInfo(LAYER_1H_COMBAT).shortNameHash == animID1HAttack3 ||
                targetAnimator.GetCurrentAnimatorStateInfo(LAYER_1H_COMBAT).shortNameHash == animID1HAttack4 ||
                targetAnimator.GetCurrentAnimatorStateInfo(LAYER_1H_COMBAT).shortNameHash == animID1HReverseAttack3 ||
                targetAnimator.GetCurrentAnimatorStateInfo(LAYER_1H_COMBAT).shortNameHash == animID1HReverseAttack4 ||
                targetAnimator.GetCurrentAnimatorStateInfo(LAYER_2H_COMBAT).shortNameHash == animID2HAttack1 ||
                targetAnimator.GetCurrentAnimatorStateInfo(LAYER_2H_COMBAT).shortNameHash == animID2HAttack2 ||
                targetAnimator.GetCurrentAnimatorStateInfo(LAYER_2H_COMBAT).shortNameHash == animID2HAttack3 ||
                targetAnimator.GetCurrentAnimatorStateInfo(LAYER_2H_COMBAT).shortNameHash == animID2HReverseAttack3 ||
                targetAnimator.GetCurrentAnimatorStateInfo(LAYER_2H_COMBAT).shortNameHash == animID2HAttack4 ||
                targetAnimator.GetCurrentAnimatorStateInfo(LAYER_2H_COMBAT).shortNameHash == animID2HReverseAttack4)
                return true;
            else
                //TODO replace with non-melee attack determination 
                return false;

        }
        else
        {
            return false;
        }
    }

    private void LookAt(Transform target)
    {
        Transform look = MatchNode.FindTransform("Head", target);
        if (look != null)
            lookTarget.transform.position = look.transform.position;
    }

    public Vector3 GetDestinationAtDesiredDistance(Transform target, float desiredDistance)
    {
        Vector3 samplePos = target.position + target.forward * desiredDistance;
        NavMeshHit navHit;
        if (NavMesh.SamplePosition(samplePos, out navHit, 0.3f, NavMesh.AllAreas))
        {
            return navHit.position;
        }
        else
        {
            for (int i = 0; i < 36; i++)
            {
                samplePos = Quaternion.AngleAxis(10, Vector3.up) * samplePos;
                if (NavMesh.SamplePosition(samplePos, out navHit, 0.3f, NavMesh.AllAreas))
                {
                    return navHit.position;
                }
            }
            return target.position;
        }
    }

    private bool IsMelee(iItem item)
    {
        if (item != null && item is iEquippable)
        {
            iEquippable equippable = (iEquippable)item;
            if (equippable.GetWeaponType() == Item.WeaponType.ONE_HANDED_SWORD ||
                equippable.GetWeaponType() == Item.WeaponType.ONE_HANDED_HAMMER ||
                equippable.GetWeaponType() == Item.WeaponType.ONE_HANDED_AXE ||
                equippable.GetWeaponType() == Item.WeaponType.GREAT_WEAPON)
                return true;
            else
                return false;
        }
        else
        {
            return true;
        }
    }

    private iCharacter GetTarget(List<iCharacter> attackers)
    {
        iCharacter closestTarget = null;
        float closestDistance = float.MaxValue;

        foreach (iCharacter attacker in attackers)
        {
            float currentDistance = Vector3.Distance(attacker.GetRootTransform().position, transform.position);

            if (currentDistance < closestDistance)
            {
                closestTarget = attacker;
                closestDistance = currentDistance;
            }
            else if (Mathf.Approximately(currentDistance, closestDistance))
            {
                if (attacker.GetCharacterState().Health < closestTarget.GetCharacterState().Health)
                    closestTarget = attacker;
            }
        }

        return closestTarget;
    }


    public void HandleLookingAround()
    {
        sleepTimer -= Time.deltaTime;

        if (sleepTimer <= 0)
        {
            LookingAround();
            Interact();
            sleepTimer = SLEEP_TIMER;
        }

        //Will prioritize combat
        if (character.IsInCombat())
        {
            lastLookAtTarget = null;
            if (combatTarget == null)
                agent.SetDestination(transform.position);
        }
    }

    private void Interact()
    {
        if (agent.remainingDistance <= agent.stoppingDistance && lastLookAtTarget != null)
        {
            Collider targetCollider = lastLookAtTarget.GetComponentInChildren<Collider>();

            if (targetCollider != null && GetShortestDirectionToRemoveOverlap(myCollider, targetCollider) != Vector3.zero)
                RepositionNavMeshAgent(agent, agent.destination);

            iInteractable interactable = lastLookAtTarget.GetComponent<iInteractable>();
            if (interactable != null)
            {
                InteractWithItem(interactable);
                InteractWithDevice(interactable);
                InteractWithDoor(interactable);
            }
            else
            {
                //TODO interact with player - use hostility score
                //TODO interact with enemy - attack
                //iDialogueUIController controller = hit.transform.GetComponentInChildren<iDialogueUIController>();

                //if (controller != null)
                //{
                //    controller.CreateDialogue();
                //}
            }
        }
    }

    private void InteractWithItem(iInteractable interactable)
    {
        if (interactable is InteractableItem)
        {
            iInteractable.InteractionStatus status = interactable.Interact(character);
            if (status == iInteractable.InteractionStatus.SUCCESS)
            {
                InteractableItem item = (InteractableItem)interactable;
                ProcessFoundEquipment(item);
                ProcessFoundKey(item);
            }
            else if (status == iInteractable.InteractionStatus.FAILURE)
            {
                forgetMes.Add(new ForgettableTarget(interactable.GetGameObject(), BASE_FORGET_DURATION * 100, ForgettableTarget.ForgetableType.ITEM));
                PostInteraction(DialogueCatalog.Find(character.GetCharacterState().Race).inventoryIsFull, sadSprite);
            }
        }
    }

    private void ProcessFoundKey(InteractableItem item)
    {
        if (item.Item.itemCatalogID == (int)ItemCatalog.ItemList.IRON_KEY)
        {
            PostInteraction(DialogueCatalog.Find(character.GetCharacterState().Race).pickingUpAKey, happySprite);
            List<ForgettableTarget> rememberDoors = new List<ForgettableTarget>();
            foreach (ForgettableTarget target in forgetMes)
            {
                if (target.forgetableType == ForgettableTarget.ForgetableType.DOOR)
                {
                    InteractableDoor door = target.forgetThisObject.GetComponentInChildren<InteractableDoor>();
                    if (door.IsDoorLocked())
                        rememberDoors.Add(target);
                }
            }

            foreach (ForgettableTarget door in rememberDoors)
                forgetMes.Remove(door);
        }
    }

    private void ProcessFoundEquipment(InteractableItem item)
    {
        if (item.Item.type == Item.Category.WEAPON || item.Item.type == Item.Category.ARMOR)
        {
            if (item.Item.type == Item.Category.WEAPON)
                PostInteraction(DialogueCatalog.Find(character.GetCharacterState().Race).pickingUpWeapon, happySprite);
            else
                PostInteraction(DialogueCatalog.Find(character.GetCharacterState().Race).pickingUpArmor, happySprite);

            foreach (Item.PaperDollSlot slot in Enum.GetValues(typeof(Item.PaperDollSlot)))
            {
                if (character.GetInventory().GetEquippedSlot(slot) == null)
                {
                    foreach (KeyValuePair<iItem, int> entry in character.GetInventory().GetItems())
                    {
                        if (entry.Key.GetDurability() > 0 && (entry.Key.GetItemType() == Item.Category.WEAPON || entry.Key.GetItemType() == Item.Category.ARMOR) && entry.Value > 0)
                        {
                            iEquippable equippable = (iEquippable)entry.Key;
                            if (equippable.GetSlot() == slot)
                                equippable.Equip(character);
                        }
                    }
                }
            }
        }
    }

    private void PostInteraction(string[] dialogueStack, AssetReferenceSprite emoji)
    {
        if (eventDialogueCountdown <= 0)
        {
            PopDialogue(dialogueStack, emoji);
            eventDialogueCountdown = EVENT_DIALOGUE_WAIT_TIME;
            Pause(SLEEP_TIMER * 5f);
        }
    }

    private void InteractWithDevice(iInteractable interactable)
    {
        if (interactable is InteractableDevice && descrInteractingWithDevice == null)
        {
            if (interactable.Interact(character) != iInteractable.InteractionStatus.OUT_OF_RANGE)
            {
                PostInteraction(DialogueCatalog.Find(character.GetCharacterState().Race).discoveryOfDevice, angrySprite);
                Pause(interactable.GetTotalInteractDuration() - interactable.GetInteractDuration());
                agent.velocity = Vector3.zero;
                descrInteractingWithDevice = LeanTween.delayedCall(interactable.GetTotalInteractDuration() - interactable.GetInteractDuration(), () =>
                {
                    //TODO when difficulty checks are put in need to test whether the NPC passed or failed the check (also need to know if the device is int or dex)
                    PostInteraction(DialogueCatalog.Find(character.GetCharacterState().Race).successfullyDisableDevice, happySprite);
                    //TODO if failed check then emote failure and forget about this device

                    if (device != null)
                    {
                        ForgettableTarget target = new ForgettableTarget(device.GetGameObject(), 1000f * BASE_FORGET_DURATION, ForgettableTarget.ForgetableType.DEVICE);
                        forgetMes.Add(target);
                    }
                });
                //need to record the position and health of the NPC, if it moves before tween is done then consider the device active and failed
                deviceStartingHealth = character.GetCharacterState().Health;
                deviceStartingPosition = character.GetRootTransform().position;
                device = (iCombatDevice)interactable;
            }
        }
    }

    private void HandleDevice()
    {
        //This is running in the main loop
        if (descrInteractingWithDevice != null)
        {
            //if moved cancel the descr and forget about the device
            if (deviceStartingHealth < character.GetCharacterState().Health || deviceStartingPosition != character.GetRootTransform().position)
            {
                LeanTween.cancel(descrInteractingWithDevice.uniqueId);
                descrInteractingWithDevice = null;
                PostInteraction(DialogueCatalog.Find(character.GetCharacterState().Race).failedDisableDevice, happySprite);
                if (device != null)
                {
                    ForgettableTarget target = new ForgettableTarget(device.GetGameObject(), 1000f * BASE_FORGET_DURATION, ForgettableTarget.ForgetableType.DEVICE);
                    forgetMes.Add(target);
                }
            }
        }
    }

    private void InteractWithDoor(iInteractable interactable)
    {
        if (interactable is InteractableDoor)
        {
            InteractableDoor door = (InteractableDoor)interactable;
            if (!door.IsDoorOpen() && !door.IsDoorLocked())
            {
                iInteractable.InteractionStatus status = door.Interact(character);
                if (status == iInteractable.InteractionStatus.SUCCESS)
                {
                    //Could be unlocked
                    PostInteraction(DialogueCatalog.Find(character.GetCharacterState().Race).unLockedDoor, happySprite);
                    forgetMes.Add(new ForgettableTarget(interactable.GetGameObject(), BASE_FORGET_DURATION * 1000, ForgettableTarget.ForgetableType.DOOR));
                    Pause(SLEEP_TIMER * 10f);
                }

            }
            else if (!door.IsDoorOpen() && door.IsDoorLocked())
            {
                foreach (KeyValuePair<iItem, int> entry in character.GetInventory().GetItems())
                {
                    if (entry.Key.GetCatalogID() == (int)ItemCatalog.ItemList.IRON_KEY && entry.Value > 0)
                    {
                        iInteractable.InteractionStatus status = door.Interact(character);
                        if (status == iInteractable.InteractionStatus.FAILURE)
                        {
                            character.GetInventory().Remove(new KeyValuePair<iItem, int>(entry.Key, 1));
                            PostInteraction(DialogueCatalog.Find(character.GetCharacterState().Race).unLockedDoor, happySprite);
                            status = door.Interact(character);
                            if (status == iInteractable.InteractionStatus.SUCCESS)
                            {
                                PostInteraction(DialogueCatalog.Find(character.GetCharacterState().Race).openingDoor, happySprite);
                                forgetMes.Add(new ForgettableTarget(interactable.GetGameObject(), BASE_FORGET_DURATION * 1000, ForgettableTarget.ForgetableType.DOOR));
                                Pause(SLEEP_TIMER * 10f);
                            }
                        }
                        break;
                    }
                }
            }
            else if (door.IsDoorOpen())
            {
                //Forget it for now, it is open, might need to be notified if it is closed
                forgetMes.Add(new ForgettableTarget(interactable.GetGameObject(), BASE_FORGET_DURATION * 1000, ForgettableTarget.ForgetableType.DOOR));
                Pause(SLEEP_TIMER * 10f);
            }
        }
    }

    private void Pause(float time)
    {
        sleepTimer = time;
        agent.isStopped = true;
        LeanTween.delayedCall(sleepTimer, () =>
        {
            if (agent.enabled != false)
                agent.isStopped = false;
        });
    }

    public Vector3 GetShortestDirectionToRemoveOverlap(Collider col1, Collider col2)
    {
        Vector3 direction = Vector3.zero;

        // Check if the colliders are overlapping
        if (col1.bounds.Intersects(col2.bounds))
        {
            // Get the center of the overlapping area
            Vector3 center = Vector3.Lerp(col1.bounds.center, col2.bounds.center, 0.5f);

            // Calculate the direction from the center of col1 to the center of the overlapping area
            direction = center - col1.bounds.center;
        }

        return direction;
    }

    private void LookingAround()
    {
        if (agent.enabled && !agent.isStopped && !character.IsInCombat())
        {
            //look for things to interact with in my sphere of awareness
            RaycastHit[] all = Physics.SphereCastAll(character.GetRootTransform().position, character.GetSphereOfAwareness(), Vector3.up, 0, lookForMask);

            List<RaycastHit> hits = new List<RaycastHit>(all);
            hits.Sort((x, y) =>
            {
                int layerPriorityX = Array.IndexOf(layerOrder, x.collider.gameObject.layer);
                int layerPriorityY = Array.IndexOf(layerOrder, y.collider.gameObject.layer);
                int layerDifference = layerPriorityX.CompareTo(layerPriorityY);

                // If layer order is not equal, return the result based on the layer order
                if (layerDifference != 0)
                {
                    return layerDifference;
                }

                // If layer order is equal, return the result based on the distance
                return x.distance.CompareTo(y.distance);
            });

            Transform currentInterest = null;

            foreach (RaycastHit hit in hits)
            {
                if (hit.transform == character.GetRootTransform() || Contains(hit.transform.gameObject) || hit.collider.isTrigger)
                    continue;

                RaycastHit lineOfSightHit;
                Vector3 center = myCollider.bounds.center;
                Vector3 size = myCollider.bounds.size;
                Vector3 forward = character.GetRootTransform().rotation * Vector3.forward;
                float projectionMagnitude = Vector3.Dot(size, forward);
                Vector3 forwardBounds = forward.normalized * projectionMagnitude;
                Vector3 finalForwardBounds = center + (forwardBounds * 1.01f);
                Debug.DrawLine(finalForwardBounds, hit.transform.position, Color.red);
                if (Physics.Linecast(finalForwardBounds, hit.transform.position, out lineOfSightHit, lineOfSightMask))
                {
                    if (lineOfSightHit.transform != hit.transform)
                    {
                        continue;
                    }
                }

                lookTarget.transform.position = hit.transform.position;
                currentInterest = hit.transform;

                if (hit.transform.gameObject.layer == layerPlayer || hit.transform.gameObject.layer == layerNPC ||
                     hit.transform.gameObject.layer == layerEnemy)
                {
                    Transform look = MatchNode.FindTransform("Head", hit.transform);
                    if (look != null)
                        lookTarget.transform.position = look.transform.position;
                    break;
                }
            }

            if (currentInterest != null && (lastLookAtTarget == null || lastLookAtTarget.position != lookTarget.position))
            {
                lastLookAtTarget = currentInterest;
                SetNavMeshDestination(lookTarget.position);
            }
            else if (currentInterest == null && agent.remainingDistance <= agent.stoppingDistance)
            {
                SetNavMeshDestination(GetRandomPosition());
                SetRandomGazeTarget();
            }
        }
    }

    private void SetRandomGazeTarget()
    {
        float maxGazeDistance = 10f;
        Vector3 modelPosition = transform.position;
        Vector3 gazeDirection = transform.forward;

        Vector3 randomGazePosition = (modelPosition + gazeDirection * Random.Range(1f, maxGazeDistance)) + (Vector3.up * 1.75f);

        Vector3 gazeVector = randomGazePosition - modelPosition;
        lookTarget.transform.position = randomGazePosition;
        lookTarget.transform.forward = gazeVector.normalized;
    }


    private bool Contains(GameObject checkMe)
    {
        foreach (ForgettableTarget forgettableTarget in forgetMes)
            if (forgettableTarget.forgetThisObject == checkMe)
                return true;
        return false;
    }

    private void RepositionNavMeshAgent(NavMeshAgent agent, Vector3 destination)
    {
        NavMeshHit hit;
        Vector3 adjustedDestination = destination;

        if (NavMesh.SamplePosition(destination, out hit, agent.radius * 2, NavMesh.AllAreas))
        {
            adjustedDestination = hit.position;
        }
        else
        {
            float maxDistance = float.MaxValue;
            Vector3 direction = destination - agent.transform.position;

            for (int i = 0; i < 360; i += 10)
            {
                Vector3 rotation = Quaternion.Euler(0, i, 0) * direction;
                Vector3 position = agent.transform.position + rotation;

                if (NavMesh.SamplePosition(position, out hit, agent.radius * 2, NavMesh.AllAreas))
                {
                    float distance = Vector3.Distance(hit.position, destination);
                    if (distance < maxDistance)
                    {
                        maxDistance = distance;
                        adjustedDestination = hit.position;
                    }
                }
            }
        }

        agent.SetDestination(adjustedDestination);
    }

    private void SetNavMeshDestination(Vector3 targetPosition)
    {
        if (NavMesh.SamplePosition(targetPosition, out navMeshHit, 1f, NavMesh.AllAreas) &&
            Vector3.Distance(targetPosition, character.GetRootTransform().position) > agent.stoppingDistance)
        {
            agent.SetDestination(navMeshHit.position);
        }
        else
        {
            if (lastLookAtTarget != null)
                forgetMes.Add(new ForgettableTarget(lastLookAtTarget.gameObject, BASE_FORGET_DURATION, ForgettableTarget.ForgetableType.UNCLEAR));
        }
    }

    public void HandleDialogue()
    {
        eventDialogueCountdown -= Time.deltaTime;
        randomDialogueCountdown -= Time.deltaTime;
        if (randomDialogueCountdown <= 0)
        {
            string[] dialogue = DialogueCatalog.Find(character.GetCharacterState().Race).general;
            PopDialogue(dialogue, sadSprite);
        }
    }

    public void PopDialogue(string[] dialogue, Emotion emotion)
    {
        switch (emotion)
        {
            case Emotion.HAPPY:
                PopDialogue(dialogue, happySprite);
                break;
            case Emotion.SAD:
                PopDialogue(dialogue, sadSprite);
                break;
            case Emotion.HURT:
                PopDialogue(dialogue, hurtSprite);
                break;
            case Emotion.ANGRY:
                PopDialogue(dialogue, angrySprite);
                break;
            case Emotion.DEAD:
                PopDialogue(dialogue, deadSprite);
                break;
        }
    }

    private void PopDialogue(string[] dialogue, AssetReferenceSprite sprite)
    {
        if (dialogue != null && dialogue.Length > 0)
        {
            iAnimationExecutor animationExecutor = popupText.GetComponent<OpenWorldDialogueAnimationExecutor>();
            popupText.SetActive(true);
            animationExecutor.Forward(popupText);

            int index = UnityEngine.Random.Range(0, dialogue.Length);
            string text = (dialogue[index] == null || dialogue[index] == "") ? dialogue[0] : dialogue[index];

            popupText.GetComponentInChildren<TextMeshProUGUI>().text = text;

            if (emoji == null)
                emoji = popupText.GetComponentsInChildren<Image>()[popupText.GetComponentsInChildren<Image>().Length - 1];

            emoji.sprite = AddressableUtility.InstantiateSprite(sprite);

            LTDescr descr = LeanTween.delayedCall(POPUP_TIME - iAnimationExecutor.TOTAL_ANIMATION_TIME, () => { animationExecutor.Reverse(); });
            descr.setOnComplete(() =>
            {
                AddressableUtility.ReleaseSprite(sprite);
                popupText.SetActive(false);
            });

            randomDialogueCountdown = UnityEngine.Random.Range(MIN_DIALOGUE_WAIT_TIME, MAX_DIALOGUE_WAIT_TIME);
        }
    }

    private Vector3 GetRandomPosition()
    {
        int maxTries = 100;
        int tryCount = 0;

        while (tryCount < maxTries)
        {
            Vector3 randomPosition;
            if (character.IsSetToFollow() && player != null)
            {
                randomPosition = player.GetRootTransform().position + new Vector3(
                     Random.Range(-xBound, xBound),
                     Random.Range(-yBound, yBound),
                     Random.Range(-zBound, zBound)
                 );
            }
            else
            {
                randomPosition = character.GetRootTransform().position + new Vector3(
                    Random.Range(-xBound, xBound),
                    Random.Range(-yBound, yBound),
                    Random.Range(-zBound, zBound)
                );
            }

            RaycastHit floorHit;
            if (Physics.Raycast(randomPosition + (Vector3.up * 10f), Vector3.down, out floorHit, 20f, floorMask))
            {
                randomPosition.y = floorHit.point.y;
            }
            if (floorHit.collider == null && Physics.Raycast(randomPosition + (Vector3.up * 10f), Vector3.up, out floorHit, 20f, floorMask))
            {
                randomPosition.y = floorHit.point.y;
            }

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPosition, out hit, 1.0f, NavMesh.AllAreas))
            {
                return hit.position;
            }

            tryCount++;
        }

        return character.GetRootTransform().position;
    }

    public void HandleForgetMe()
    {
        for (int i = forgetMes.Count - 1; i >= forgetMes.Count; i--)
        {
            forgetMes[i].forgetDuration -= Time.deltaTime;

            if (forgetMes[i].forgetDuration <= 0)
            {
                forgetMes.RemoveAt(i);
            }
        }
    }

    private class ForgettableTarget
    {
        public enum ForgetableType
        {
            UNCLEAR = 0,
            PLAYER = 1,
            ITEM = 2,
            KEY = 3,
            EQUIPPABLE = 4,
            DEVICE = 5,
            DOOR = 6
        }

        public GameObject forgetThisObject;
        public float forgetDuration;
        public ForgetableType forgetableType;

        public ForgettableTarget(GameObject forgetThisObject, float forgetDuration, ForgetableType type)
        {
            this.forgetThisObject = forgetThisObject;
            this.forgetDuration = forgetDuration;
            forgetableType = type;
        }
    }
}
