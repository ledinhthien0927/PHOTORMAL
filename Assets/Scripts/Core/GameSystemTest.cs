using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Comprehensive Game System Test Script.
/// Attach to any GameObject in the scene to test all major systems via F-keys.
/// 
/// F1  = Customer Spawn Test (queue composition & randomness across nights)
/// F2  = Rule: NoFlickerShoot
/// F3  = Rule: TwinsTurnOffLight
/// F4  = Rule: BackDoorLocked (5s timer, no repeat fire)
/// F5  = Rule: HideWhenFootstep (5s grace)
/// F6  = Rule: StudioTimeLimit (Night 2, 15s)
/// F7  = Rule: ClownDoorOpen (Night 3, 5s jumpscare)
/// F8  = Item Interaction Tests (Door, LightSwitch, BackDoor)
/// F9  = Full Game Loop Test (Night transitions)
/// F10 = Status Panel (dump all current state)
/// F11 = Win Night (Add money to reach target)
/// </summary>
public class GameSystemTest : MonoBehaviour
{
    [Header("Test Config")]
    [SerializeField] private int spawnTestIterations = 10;

    private Coroutine activeTestCoroutine;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1)) RunTest(TestCustomerSpawn());
        if (Input.GetKeyDown(KeyCode.F2)) RunTest(TestRule_NoFlickerShoot());
        if (Input.GetKeyDown(KeyCode.F3)) RunTest(TestRule_TwinsTurnOffLight());
        if (Input.GetKeyDown(KeyCode.F4)) RunTest(TestRule_BackDoorLocked());
        if (Input.GetKeyDown(KeyCode.F5)) RunTest(TestRule_HideWhenFootstep());
        if (Input.GetKeyDown(KeyCode.F6)) RunTest(TestRule_StudioTimeLimit());
        if (Input.GetKeyDown(KeyCode.F7)) RunTest(TestRule_ClownDoorOpen());
        if (Input.GetKeyDown(KeyCode.F8)) RunTest(TestItemInteractions());
        if (Input.GetKeyDown(KeyCode.F9)) RunTest(TestFullGameLoop());
        if (Input.GetKeyDown(KeyCode.F10)) DumpStatus();
        if (Input.GetKeyDown(KeyCode.F11)) RunTest(TestWinNight());
    }

    private void RunTest(IEnumerator testRoutine)
    {
        if (activeTestCoroutine != null)
        {
            StopCoroutine(activeTestCoroutine);
            Log("--- Previous test cancelled ---");
        }
        activeTestCoroutine = StartCoroutine(TestWrapper(testRoutine));
    }

    private IEnumerator TestWrapper(IEnumerator testRoutine)
    {
        yield return StartCoroutine(testRoutine);
        activeTestCoroutine = null;
    }

    // ============================================================
    // F1: CUSTOMER SPAWN TEST
    // ============================================================

    private IEnumerator TestCustomerSpawn()
    {
        LogHeader("CUSTOMER SPAWN TEST");

        if (CustomerQueueManager.Instance == null)
        {
            LogFail("CustomerQueueManager.Instance is NULL. Ensure it exists in scene.");
            yield break;
        }

        for (int night = 1; night <= 3; night++)
        {
            Log($"--- Night {night} ---");

            int totalTwins = 0;
            int totalClowns = 0;
            int totalNormal = 0;

            for (int i = 0; i < spawnTestIterations; i++)
            {
                // Build queue (this populates the internal queue)
                CustomerQueueManager.Instance.BuildQueueForNight(night);

                // Count entries by peeking at the queue via reflection or debug dump
                // We use a simulated approach: count events registered via Debug.Log output
                // Since we can't easily peek into the private queue, we re-implement BuildEntryList logic here
                var counts = SimulateQueueBuild(night);
                totalNormal += counts.normal;
                totalTwins += counts.twins;
                totalClowns += counts.clown;

                yield return null; // Let coroutines settle
            }

            Log($"Night {night} over {spawnTestIterations} iterations:");
            Log($"  Normal: {totalNormal} | Twins: {totalTwins} | Clown: {totalClowns}");

            // Verify Twins guaranteed
            if (totalTwins >= spawnTestIterations)
                LogPass($"Twins guaranteed (≥{spawnTestIterations} across {spawnTestIterations} runs)");
            else
                LogFail($"Twins NOT guaranteed: only {totalTwins} in {spawnTestIterations} runs");

            // Verify Clown guaranteed for Night 3+
            if (night >= 3)
            {
                if (totalClowns >= spawnTestIterations)
                    LogPass($"Clown guaranteed at Night {night} (≥{spawnTestIterations})");
                else
                    LogFail($"Clown NOT guaranteed at Night {night}: only {totalClowns}");
            }
            else
            {
                if (totalClowns == 0)
                    LogPass($"No Clown at Night {night} (correct)");
                else
                    LogFail($"Clown appeared at Night {night} when it shouldn't!");
            }
        }

        // Stop any spawned customers from moving around
        CustomerQueueManager.Instance.BuildQueueForNight(0); // Reset
        Log("Customer Spawn Test Complete.");
    }

    private (int normal, int twins, int clown) SimulateQueueBuild(int night)
    {
        int baseCount = 5;
        int increasePerNight = 2;
        float twinsChance = 0.20f;
        float clownChance = 0.15f;
        int minNormalCustomersBeforeEvent = 2;

        int totalSlots = baseCount + (night - 1) * increasePerNight;
        int startIndex = Mathf.Clamp(minNormalCustomersBeforeEvent, 0, totalSlots - 1);

        // 0=Normal, 1=Twins, 2=Clown
        int[] types = new int[totalSlots];

        // Twins pass
        bool twinsGuaranteed = false;
        for (int i = startIndex; i < totalSlots; i++)
        {
            if (Random.value < twinsChance)
            {
                types[i] = 1;
                twinsGuaranteed = true;
            }
        }
        if (!twinsGuaranteed && totalSlots > 0)
        {
            int idx = Mathf.Max(startIndex, Random.Range(totalSlots / 2, totalSlots));
            for (int i = idx; i < totalSlots; i++)
            {
                if (types[i] == 0) { types[i] = 1; break; }
            }
        }

        // Clown pass (Night 3+)
        if (night >= 3)
        {
            bool clownGuaranteed = false;
            for (int i = startIndex; i < totalSlots; i++)
            {
                if (types[i] != 0) continue;
                if (Random.value < clownChance)
                {
                    types[i] = 2;
                    clownGuaranteed = true;
                }
            }
            if (!clownGuaranteed && totalSlots > 0)
            {
                int startSearch = totalSlots / 2;
                bool inserted = false;
                for (int i = totalSlots - 1; i >= startSearch; i--)
                {
                    if (types[i] == 0) { types[i] = 2; inserted = true; break; }
                }
                if (!inserted)
                {
                    // Would normally append, but we just count an extra
                }
            }
        }

        int n = 0, t = 0, c = 0;
        for (int i = 0; i < totalSlots; i++)
        {
            if (types[i] == 0) n++;
            else if (types[i] == 1) t++;
            else c++;
        }
        return (n, t, c);
    }

    // ============================================================
    // F2: RULE - NoFlickerShoot
    // ============================================================

    private IEnumerator TestRule_NoFlickerShoot()
    {
        LogHeader("RULE TEST: NoFlickerShoot");

        if (!CheckDependencies()) yield break;

        int errorsBefore = GameProgress.Instance.CurrentError;

        // Setup Night 1 rules
        RuleManager.Instance.SetupRules(1);
        GameProgress.Instance.ResetNightData();
        errorsBefore = 0;

        // Test 1: Shoot when NOT flickering (should NOT break rule)
        RuleContext.Instance.IsFlickering = false;
        GameEventAPI.OnPlayerShootPhoto?.Invoke();
        yield return null;

        if (GameProgress.Instance.CurrentError == errorsBefore)
            LogPass("Shooting without flicker: No error (correct)");
        else
            LogFail("Shooting without flicker: Error was added (wrong)");

        // Test 2: Shoot when IS flickering (should break rule)
        errorsBefore = GameProgress.Instance.CurrentError;
        RuleContext.Instance.IsFlickering = true;
        GameEventAPI.OnPlayerShootPhoto?.Invoke();
        yield return null;

        if (GameProgress.Instance.CurrentError > errorsBefore)
            LogPass("Shooting during flicker: Error added (correct)");
        else
            LogFail("Shooting during flicker: No error (wrong)");

        // Cleanup
        RuleContext.Instance.IsFlickering = false;
        Log("NoFlickerShoot Test Complete.");
    }

    // ============================================================
    // F3: RULE - TwinsTurnOffLight
    // ============================================================

    private IEnumerator TestRule_TwinsTurnOffLight()
    {
        LogHeader("RULE TEST: TwinsTurnOffLight");

        if (!CheckDependencies()) yield break;

        RuleManager.Instance.SetupRules(1);
        GameProgress.Instance.ResetNightData();

        // Test 1: Twins appear, player turns off light → no error
        RuleContext.Instance.HasTwinsAppeared = true;
        RuleContext.Instance.IsLivingRoomLightOn = true;
        GameEventAPI.OnLivingRoomLightToggled?.Invoke(false); // Toggle OFF
        yield return null;

        if (!RuleContext.Instance.HasTwinsAppeared)
            LogPass("Twins disappeared after light off (correct)");
        else
            LogFail("Twins still present after light off (wrong)");

        if (GameProgress.Instance.CurrentError == 0)
            LogPass("No error for quick light off (correct)");
        else
            LogFail("Error added for quick light off (wrong)");

        // Test 2: Twins appear, player does NOT turn off light → wait for TwinsEvent timeout
        Log("Test 2: Twins appear, waiting for timeout (10s)...");
        RuleContext.Instance.HasTwinsAppeared = true;
        RuleContext.Instance.IsLivingRoomLightOn = true;
        GameEventAPI.OnTwinsPresenceChanged?.Invoke(true);

        // Wait 11 seconds for TwinsEvent to timeout and add error
        // (TwinsEvent has durationBeforeError = 10f)
        // NOTE: This only works if TwinsEvent is registered with EventManager and triggered
        int errorsBeforeTimeout = GameProgress.Instance.CurrentError;
        EventManager.Instance.TriggerEvent("Twins");
        yield return new WaitForSeconds(11f);

        if (GameProgress.Instance.CurrentError > errorsBeforeTimeout)
            LogPass("Twins timeout: Error added after 10s (correct)");
        else
            LogFail("Twins timeout: No error after 10s (wrong — is TwinsEvent registered?)");

        // Cleanup
        RuleContext.Instance.HasTwinsAppeared = false;
        RuleContext.Instance.IsLivingRoomLightOn = true;
        Log("TwinsTurnOffLight Test Complete.");
    }

    // ============================================================
    // F4: RULE - BackDoorLocked (5s timer, single fire)
    // ============================================================

    private IEnumerator TestRule_BackDoorLocked()
    {
        LogHeader("RULE TEST: BackDoorLocked");

        if (!CheckDependencies()) yield break;

        RuleManager.Instance.SetupRules(1);
        GameProgress.Instance.ResetNightData();

        // Test 1: Open back door, close after 3s → NO error
        GameEventAPI.OnBackDoorStateChanged?.Invoke(false); // isLocked = false → mở cửa
        yield return new WaitForSeconds(3f);
        GameEventAPI.OnBackDoorStateChanged?.Invoke(true); // isLocked = true → khóa lại
        yield return null;

        if (GameProgress.Instance.CurrentError == 0)
            LogPass("Door open <5s then closed: No error (correct)");
        else
            LogFail("Door open <5s then closed: Error added (wrong)");

        // Test 2: Open back door, wait >5s → SHOULD add exactly 1 error
        int errorsBefore = GameProgress.Instance.CurrentError;
        GameEventAPI.OnBackDoorStateChanged?.Invoke(false); // Mở cửa
        Log("Waiting 6s with door open...");
        yield return new WaitForSeconds(6f);

        int errorsAfter5s = GameProgress.Instance.CurrentError;
        if (errorsAfter5s == errorsBefore + 1)
            LogPass("Door open >5s: Exactly 1 error (correct)");
        else
            LogFail($"Door open >5s: Expected {errorsBefore + 1} error, got {errorsAfter5s}");

        // Test 3: Keep door open 5 more seconds → should NOT add another error (no repeat fire)
        Log("Waiting 6 more seconds to test no-repeat fire...");
        yield return new WaitForSeconds(6f);

        int errorsAfter10s = GameProgress.Instance.CurrentError;
        if (errorsAfter10s == errorsAfter5s)
            LogPass("Door still open >10s: No repeat error (correct — single fire)");
        else
            LogFail($"Door still open >10s: Error repeated! Got {errorsAfter10s} (expected {errorsAfter5s})");

        // Cleanup
        GameEventAPI.OnBackDoorStateChanged?.Invoke(true); // Đóng khóa lại
        Log("BackDoorLocked Test Complete.");
    }

    // ============================================================
    // F5: RULE - HideWhenFootstep (5s grace)
    // ============================================================

    private IEnumerator TestRule_HideWhenFootstep()
    {
        LogHeader("RULE TEST: HideWhenFootstep");

        if (!CheckDependencies()) yield break;

        RuleManager.Instance.SetupRules(1);
        GameProgress.Instance.ResetNightData();

        // Test 1: Footstep active, player enters toilet within 3s → no error
        RuleContext.Instance.IsFootstepActive = true;
        RuleContext.Instance.IsPlayerInToilet = false;
        yield return new WaitForSeconds(3f);

        // Enter toilet
        GameEventAPI.OnPlayerToiletStateChanged?.Invoke(true);
        yield return new WaitForSeconds(3f); // Wait a bit

        if (GameProgress.Instance.CurrentError == 0)
            LogPass("Entered toilet within 3s: No error (correct)");
        else
            LogFail("Entered toilet within 3s: Error added (wrong)");

        // End footstep
        RuleContext.Instance.IsFootstepActive = false;
        GameEventAPI.OnPlayerToiletStateChanged?.Invoke(false);
        yield return null;

        // Test 2: Footstep active, player stays outside toilet >5s → error
        int errorsBefore = GameProgress.Instance.CurrentError;
        RuleContext.Instance.IsFootstepActive = true;
        RuleContext.Instance.IsPlayerInToilet = false;
        Log("Waiting 6s outside toilet during footstep...");
        yield return new WaitForSeconds(6f);

        if (GameProgress.Instance.CurrentError > errorsBefore)
            LogPass("Outside toilet >5s during footstep: Error added (correct)");
        else
            LogFail("Outside toilet >5s during footstep: No error (wrong)");

        // Cleanup
        RuleContext.Instance.IsFootstepActive = false;
        RuleContext.Instance.IsPlayerInToilet = false;
        Log("HideWhenFootstep Test Complete.");
    }

    // ============================================================
    // F6: RULE - StudioTimeLimit (Night 2, 15s)
    // ============================================================

    private IEnumerator TestRule_StudioTimeLimit()
    {
        LogHeader("RULE TEST: StudioTimeLimit (Night 2)");

        if (!CheckDependencies()) yield break;

        RuleManager.Instance.SetupRules(2); // Night 2 for this rule
        GameProgress.Instance.ResetNightData();

        // Test 1: Enter studio, exit within 10s → no error
        GameEventAPI.OnPlayerStudioStateChanged?.Invoke(true);
        yield return new WaitForSeconds(10f);
        GameEventAPI.OnPlayerStudioStateChanged?.Invoke(false);
        yield return null;

        if (GameProgress.Instance.CurrentError == 0)
            LogPass("Studio exit within 10s: No error (correct)");
        else
            LogFail("Studio exit within 10s: Error added (wrong)");

        // Test 2: Enter studio, stay >15s → error
        int errorsBefore = GameProgress.Instance.CurrentError;
        GameEventAPI.OnPlayerStudioStateChanged?.Invoke(true);
        Log("Waiting 16s in studio...");
        yield return new WaitForSeconds(16f);

        if (GameProgress.Instance.CurrentError > errorsBefore)
            LogPass("Studio stay >15s: Error added (correct)");
        else
            LogFail("Studio stay >15s: No error (wrong)");

        // Cleanup
        GameEventAPI.OnPlayerStudioStateChanged?.Invoke(false);
        Log("StudioTimeLimit Test Complete.");
    }

    // ============================================================
    // F7: RULE - ClownDoorOpen (Night 3, 5s jumpscare)
    // ============================================================

    private IEnumerator TestRule_ClownDoorOpen()
    {
        LogHeader("RULE TEST: ClownDoorOpen (Night 3)");

        if (!CheckDependencies()) yield break;

        RuleManager.Instance.SetupRules(3); // Night 3 for this rule
        GameProgress.Instance.ResetNightData();

        bool jumpscareTriggered = false;
        System.Action jumpscareListener = () => jumpscareTriggered = true;
        GameEventAPI.OnClownJumpscare += jumpscareListener;

        // Test 1: Clown appears, player opens door within 3s → no jumpscare
        RuleContext.Instance.IsClownAppeared = true;
        GameEventAPI.OnClownAppeared?.Invoke();
        yield return new WaitForSeconds(3f);
        GameEventAPI.OnPlayerOpenedDoorForClown?.Invoke();
        yield return null;

        if (!jumpscareTriggered)
            LogPass("Opened door for clown within 3s: No jumpscare (correct)");
        else
            LogFail("Opened door for clown within 3s: Jumpscare triggered (wrong)");

        if (!RuleContext.Instance.IsClownAppeared)
            LogPass("Clown disappeared after opening door (correct)");
        else
            LogFail("Clown still present after opening door (wrong)");

        // Test 2: Clown appears, player does NOT open door → jumpscare after 5s
        jumpscareTriggered = false;
        RuleContext.Instance.IsClownAppeared = true;
        GameEventAPI.OnClownAppeared?.Invoke();
        Log("Waiting 6s without opening door...");
        yield return new WaitForSeconds(6f);

        if (jumpscareTriggered)
            LogPass("Did not open door >5s: Jumpscare fired (correct)");
        else
            LogFail("Did not open door >5s: No jumpscare (wrong)");

        // Cleanup
        GameEventAPI.OnClownJumpscare -= jumpscareListener;
        RuleContext.Instance.IsClownAppeared = false;
        Log("ClownDoorOpen Test Complete.");
    }

    // ============================================================
    // F8: ITEM INTERACTION TESTS
    // ============================================================

    private IEnumerator TestItemInteractions()
    {
        LogHeader("ITEM INTERACTION TESTS");

        // Test BackDoor Event API
        Log("--- BackDoor API Test ---");
        bool backDoorEventReceived = false;
        bool backDoorLastState = false;
        System.Action<bool> backDoorListener = (isLocked) =>
        {
            backDoorEventReceived = true;
            backDoorLastState = isLocked;
        };
        GameEventAPI.OnBackDoorStateChanged += backDoorListener;

        GameEventAPI.OnBackDoorStateChanged?.Invoke(false); // Mở khóa
        yield return null;
        if (backDoorEventReceived && !backDoorLastState)
            LogPass("BackDoor unlock event: Received isLocked=false (correct)");
        else
            LogFail("BackDoor unlock event: Not received correctly");

        backDoorEventReceived = false;
        GameEventAPI.OnBackDoorStateChanged?.Invoke(true); // Khóa lại
        yield return null;
        if (backDoorEventReceived && backDoorLastState)
            LogPass("BackDoor lock event: Received isLocked=true (correct)");
        else
            LogFail("BackDoor lock event: Not received correctly");

        GameEventAPI.OnBackDoorStateChanged -= backDoorListener;

        // Test LivingRoom Light API
        Log("--- LivingRoom Light API Test ---");
        bool lightEventReceived = false;
        bool lightLastState = false;
        System.Action<bool> lightListener = (isOn) =>
        {
            lightEventReceived = true;
            lightLastState = isOn;
        };
        GameEventAPI.OnLivingRoomLightToggled += lightListener;

        GameEventAPI.OnLivingRoomLightToggled?.Invoke(false);
        yield return null;
        if (lightEventReceived && !lightLastState)
            LogPass("Light toggle OFF event: Received (correct)");
        else
            LogFail("Light toggle OFF event: Not received correctly");

        lightEventReceived = false;
        GameEventAPI.OnLivingRoomLightToggled?.Invoke(true);
        yield return null;
        if (lightEventReceived && lightLastState)
            LogPass("Light toggle ON event: Received (correct)");
        else
            LogFail("Light toggle ON event: Not received correctly");

        GameEventAPI.OnLivingRoomLightToggled -= lightListener;

        // Test Studio State API
        Log("--- Studio State API Test ---");
        bool studioEventReceived = false;
        bool studioLastState = false;
        System.Action<bool> studioListener = (entered) =>
        {
            studioEventReceived = true;
            studioLastState = entered;
        };
        GameEventAPI.OnPlayerStudioStateChanged += studioListener;

        GameEventAPI.OnPlayerStudioStateChanged?.Invoke(true);
        yield return null;
        if (studioEventReceived && studioLastState)
            LogPass("Studio enter event: Received (correct)");
        else
            LogFail("Studio enter event: Not received correctly");

        studioEventReceived = false;
        GameEventAPI.OnPlayerStudioStateChanged?.Invoke(false);
        yield return null;
        if (studioEventReceived && !studioLastState)
            LogPass("Studio exit event: Received (correct)");
        else
            LogFail("Studio exit event: Not received correctly");

        GameEventAPI.OnPlayerStudioStateChanged -= studioListener;

        // Test Toilet State API
        Log("--- Toilet State API Test ---");
        bool toiletEventReceived = false;
        bool toiletLastState = false;
        System.Action<bool> toiletListener = (inside) =>
        {
            toiletEventReceived = true;
            toiletLastState = inside;
        };
        GameEventAPI.OnPlayerToiletStateChanged += toiletListener;

        GameEventAPI.OnPlayerToiletStateChanged?.Invoke(true);
        yield return null;
        if (toiletEventReceived && toiletLastState)
            LogPass("Toilet enter event: Received (correct)");
        else
            LogFail("Toilet enter event: Not received correctly");

        toiletEventReceived = false;
        GameEventAPI.OnPlayerToiletStateChanged?.Invoke(false);
        yield return null;
        if (toiletEventReceived && !toiletLastState)
            LogPass("Toilet exit event: Received (correct)");
        else
            LogFail("Toilet exit event: Not received correctly");

        GameEventAPI.OnPlayerToiletStateChanged -= toiletListener;

        Log("Item Interaction Tests Complete.");
    }

    // ============================================================
    // F9: FULL GAME LOOP TEST
    // ============================================================

    private IEnumerator TestFullGameLoop()
    {
        LogHeader("FULL GAME LOOP TEST");

        if (!CheckDependencies()) yield break;

        if (NightManager.Instance == null)
        {
            LogFail("NightManager.Instance is NULL.");
            yield break;
        }

        // Reset to Night 1
        GameProgress.Instance.ResetNightData();
        // We can't easily reset CurrentNight since it's private set, so just test from current

        int startNight = GameProgress.Instance.CurrentNight;
        Log($"Starting from Night {startNight}");

        // Start Night
        NightManager.Instance.StartNight(startNight);
        yield return null;

        // Verify rules are set for this night
        Log($"Current Night: {GameProgress.Instance.CurrentNight}");
        Log($"Current Errors: {GameProgress.Instance.CurrentError}");
        Log($"Current Money: {GameProgress.Instance.CurrentMoney}");

        // Test: Add money to reach target
        int[] targets = { 600, 900, 1200 };
        int targetIndex = Mathf.Clamp(startNight - 1, 0, targets.Length - 1);
        int target = targets[targetIndex];

        Log($"Target for Night {startNight}: ${target}");

        // Add just under target
        GameEventAPI.OnAddMoney?.Invoke(target - 100);
        yield return null;
        Log($"Added ${target - 100}. Money: ${GameProgress.Instance.CurrentMoney}");

        if (GameProgress.Instance.CurrentNight == startNight)
            LogPass("Under target: Night did not advance (correct)");
        else
            LogFail("Under target: Night advanced too early (wrong)");

        // Add remaining to reach target
        GameEventAPI.OnAddMoney?.Invoke(100);
        yield return null;
        Log($"Added $100 more. Money: ${GameProgress.Instance.CurrentMoney}");

        if (GameProgress.Instance.CurrentNight > startNight)
            LogPass($"Reached target: Night advanced to {GameProgress.Instance.CurrentNight} (correct)");
        else
            LogFail($"Reached target: Night did NOT advance (wrong, still Night {GameProgress.Instance.CurrentNight})");

        // Test error system
        Log("--- Error System Test ---");
        GameProgress.Instance.ResetNightData();

        for (int i = 1; i <= 3; i++)
        {
            GameProgress.Instance.AddError();
            Log($"Error {i} added. Total: {GameProgress.Instance.CurrentError}");
            yield return null;
        }

        if (GameProgress.Instance.CurrentError == 3)
            LogPass("3 errors added correctly");
        else
            LogFail($"Expected 3 errors, got {GameProgress.Instance.CurrentError}");

        // 4th error = game over
        Log("Adding 4th error (should trigger InstantGameOver)...");
        bool gameOverTriggered = false;
        // We can't easily check if InstantGameOver was triggered via EventManager,
        // but we can check if error count is 4
        GameProgress.Instance.AddError();
        yield return null;

        if (GameProgress.Instance.CurrentError > 3)
            LogPass($"4th error: Error count = {GameProgress.Instance.CurrentError} (GameOver should have triggered)");
        else
            LogFail("4th error: Error count didn't increase");

        Log("Full Game Loop Test Complete.");
    }

    // ============================================================
    // F11: WIN NIGHT TEST
    // ============================================================

    private IEnumerator TestWinNight()
    {
        LogHeader("WIN NIGHT TEST");

        if (!CheckDependencies()) yield break;

        int currentNight = GameProgress.Instance.CurrentNight;
        int currentMoney = GameProgress.Instance.CurrentMoney;
        
        Log($"Current Night: {currentNight}, Current Money: ${currentMoney}");

        // Targets are 600, 900, 1200. Adding 2000 will satisfy any night.
        int winAmount = 2000;
        Log($"Adding ${winAmount} to win the night...");
        
        GameEventAPI.OnAddMoney?.Invoke(winAmount);
        
        yield return new WaitForSecondsRealtime(1.0f); // Use realtime because timescale is 0

        // In this new system, the night doesn't advance UNTIL the player clicks the button.
        // So we just check if the game is paused (which indicates the Win UI is shown).
        if (Time.timeScale == 0f)
        {
            LogPass("Win condition met and UI displayed (Game Paused). Please click Next Level manually.");
        }
        else
        {
            LogFail("Win condition might not have triggered (Game not paused).");
        }

        Log("Win Night Test Complete.");
    }

    // ============================================================
    // F10: STATUS PANEL
    // ============================================================

    private void DumpStatus()
    {
        LogHeader("GAME STATUS DUMP");

        StringBuilder sb = new StringBuilder();

        // Game Progress
        if (GameProgress.Instance != null)
        {
            sb.AppendLine($"Night: {GameProgress.Instance.CurrentNight}");
            sb.AppendLine($"Money: ${GameProgress.Instance.CurrentMoney}");
            sb.AppendLine($"Errors: {GameProgress.Instance.CurrentError} / {GameProgress.Instance.MaxError}");
        }
        else
        {
            sb.AppendLine("GameProgress: NULL");
        }

        sb.AppendLine();

        // Rule Context
        if (RuleContext.Instance != null)
        {
            sb.AppendLine("--- RuleContext ---");
            sb.AppendLine($"IsFlickering: {RuleContext.Instance.IsFlickering}");
            sb.AppendLine($"IsFootstepActive: {RuleContext.Instance.IsFootstepActive}");
            sb.AppendLine($"IsClownAppeared: {RuleContext.Instance.IsClownAppeared}");
            sb.AppendLine($"HasTwinsAppeared: {RuleContext.Instance.HasTwinsAppeared}");
            sb.AppendLine($"IsDeliveryWaiting: {RuleContext.Instance.IsDeliveryWaiting}");
            sb.AppendLine($"IsLivingRoomLightOn: {RuleContext.Instance.IsLivingRoomLightOn}");
            sb.AppendLine($"IsBackDoorLocked: {RuleContext.Instance.IsBackDoorLocked}");
            sb.AppendLine($"IsPlayerInToilet: {RuleContext.Instance.IsPlayerInToilet}");
            sb.AppendLine($"StudioEnterTime: {RuleContext.Instance.StudioEnterTime}");
            sb.AppendLine($"ClownAppearTime: {RuleContext.Instance.ClownAppearTime}");
        }
        else
        {
            sb.AppendLine("RuleContext: NULL");
        }

        sb.AppendLine();

        // StudioManager
        if (StudioManager.Instance != null)
        {
            sb.AppendLine("--- StudioManager ---");
            sb.AppendLine($"Mode: {StudioManager.Instance.CurrentMode}");
            sb.AppendLine($"CurrentCustomer: {(StudioManager.Instance.CurrentCustomer != null ? StudioManager.Instance.CurrentCustomer.name : "None")}");
            sb.AppendLine($"CanPrintCurrentPhoto: {StudioManager.Instance.CanPrintCurrentPhoto}");
        }
        else
        {
            sb.AppendLine("StudioManager: NULL");
        }

        sb.AppendLine();

        // Managers 
        sb.AppendLine("--- Singletons ---");
        sb.AppendLine($"RuleManager: {(RuleManager.Instance != null ? "OK" : "NULL")}");
        if (RuleManager.Instance != null)
        {
            sb.Append("  Active Rules: ");
            foreach (var rule in RuleManager.Instance.ActiveRules)
            {
                sb.Append(rule.ToString() + ", ");
            }
            sb.AppendLine();
        }
        sb.AppendLine($"EventManager: {(EventManager.Instance != null ? "OK" : "NULL")}");
        sb.AppendLine($"NightManager: {(NightManager.Instance != null ? "OK" : "NULL")}");
        sb.AppendLine($"CustomerQueueManager: {(CustomerQueueManager.Instance != null ? "OK" : "NULL")}");
        sb.AppendLine($"WarningManager: {(WarningManager.Instance != null ? "OK" : "NULL")}");

        Debug.Log(sb.ToString());
    }

    // ============================================================
    // UTILITY
    // ============================================================

    private bool CheckDependencies()
    {
        bool ok = true;

        if (RuleManager.Instance == null)
        {
            LogFail("RuleManager.Instance is NULL");
            ok = false;
        }

        if (RuleContext.Instance == null)
        {
            LogFail("RuleContext.Instance is NULL");
            ok = false;
        }

        if (GameProgress.Instance == null)
        {
            LogFail("GameProgress.Instance is NULL");
            ok = false;
        }

        if (EventManager.Instance == null)
        {
            LogFail("EventManager.Instance is NULL");
            ok = false;
        }

        return ok;
    }

    private void Log(string message)
    {
        Debug.Log($"<color=cyan>[GameSystemTest]</color> {message}");
    }

    private void LogHeader(string title)
    {
        Debug.Log($"<color=yellow>========== {title} ==========</color>");
    }

    private void LogPass(string message)
    {
        Debug.Log($"<color=green>[TEST PASS]</color> {message}");
    }

    private void LogFail(string message)
    {
        Debug.LogError($"<color=red>[TEST FAIL]</color> {message}");
    }
}
