using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using EnumManager;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class BossStageManagerTest
{
    private GameObject gameManagerObject;
    private GameManager gameManager;
    private GameObject bossStageManagerObject;
    private BossStageManager bossStageManager;

    [SetUp]
    public void Setup()
    {
        gameManagerObject = new GameObject("GameManager");
        gameManager = gameManagerObject.AddComponent<GameManager>();
        GameManager.inst = gameManager;

        gameManager.SetMaxLife(3);
        gameManager.SetBossStageMaxLife(5);
        gameManager.ResetStats();
        gameManager.AddLife(gameManager.GetBossStageMaxLife());

        bossStageManagerObject = new GameObject("BossStageManager");
        bossStageManager = bossStageManagerObject.AddComponent<BossStageManager>();
        bossStageManagerObject.AddComponent<BossIntroManager>();

        InitializeStageManagerFields();
        InitializeBossStageManagerFields();

        InvokeNonPublicMethod(bossStageManager, "Awake");

        SetPrivateField(bossStageManager, "playerScript", new BossStagePlayer());
    }

    [TearDown]
    public void Teardown()
    {
        Object.DestroyImmediate(bossStageManagerObject);
        Object.DestroyImmediate(gameManagerObject);
        GameManager.inst = null;
    }

    private void InitializeStageManagerFields()
    {
        bossStageManager.characters = new GameObject[1];
        bossStageManager.characters[0] = new GameObject("Character0");
        bossStageManager.characters[0].AddComponent<BossStagePlayer>();

        bossStageManager.characterUI = new GameObject[1];
        bossStageManager.characterUI[0] = new GameObject("CharacterUI0");

        SetProtectedField(bossStageManager, "activeCharacter", bossStageManager.characters[0]);

        bossStageManager.pauseMenu = new GameObject("Pause");
        bossStageManager.pauseMenu.SetActive(false);

        bossStageManager.gameOverScreen = new GameObject("GameOver");
        bossStageManager.gameOverScreen.SetActive(false);

        bossStageManager.score = new GameObject("Score");
        bossStageManager.score.AddComponent<TextMeshProUGUI>();

        bossStageManager.hearts = new GameObject[gameManager.GetBossStageMaxLife()];
        for (int i = 0; i < bossStageManager.hearts.Length; i++)
        {
            bossStageManager.hearts[i] = new GameObject($"Heart{i}");
            bossStageManager.hearts[i].SetActive(true);
        }

        MusicManager musicManager = new GameObject("MusicManager").AddComponent<MusicManager>();
    }

    private void InitializeBossStageManagerFields()
    {
        bossStageManager.darkHearts = new GameObject[bossStageManager.GetBossMaxLife()];
        for (int i = 0; i < bossStageManager.darkHearts.Length; i++)
        {
            bossStageManager.darkHearts[i] = new GameObject($"DarkHeart{i}");
            bossStageManager.darkHearts[i].SetActive(true);
        }

        bossStageManager.gameClear = new GameObject("GameClear");
        bossStageManager.gameClear.SetActive(false);
        GameObject boss = new GameObject("Boss");
        GameObject camera = new GameObject("Main Camera");
        bossStageManager.bossControlScript = camera.AddComponent<BossControl>();
        bossStageManager.cameraScript = boss.AddComponent<BossStageCamera>();

        bossStageManager.bossControlScript = new BossControl();
        bossStageManager.cameraScript = new BossStageCamera();
        bossStageManager.weakspotsManagerScript = new WeakspotsManager();

        GameObject fire1 = new GameObject("Fire1") { tag = "Fire" };
        GameObject fire2 = new GameObject("Fire2") { tag = "Fire" };
        fire1.AddComponent<AudioSource>();
        fire2.AddComponent<AudioSource>();
        GameClearLight gameClearLight = bossStageManagerObject.AddComponent<GameClearLight>();
        // fadeImageAnimator �ʱ�ȭ
        GameObject fadeImage = new GameObject("FadeImage");
        Animator animator = fadeImage.AddComponent<Animator>();
        BossStageTransitionManager transitionManager = new GameObject("BossStageTransitionManager").AddComponent<BossStageTransitionManager>();

    }

    private void SetPrivateField(object obj, string fieldName, object value)
    {
        FieldInfo field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        field.SetValue(obj, value);
    }

    private void SetProtectedField(object obj, string fieldName, object value)
    {
        FieldInfo field = obj.GetType().BaseType.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        field.SetValue(obj, value);
    }

    private void InvokeNonPublicMethod(object obj, string methodName)
    {
        MethodInfo method = obj.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        method.Invoke(obj, null);
    }

    [Test]
    public void TestBossLifeInitialization()
    {
        int bossLife = bossStageManager.GetBossLife();
        Assert.AreEqual(bossStageManager.GetBossMaxLife(), bossLife);
    }

    [Test]
    public void TestIncrementPhase()
    {
        int initialPhase = bossStageManager.GetPhase();
        Assert.AreEqual(0, initialPhase);

        bossStageManager.IncrementPhase();

        int newPhase = bossStageManager.GetPhase();
        int bossLife = bossStageManager.GetBossLife();
        Assert.AreEqual(1, newPhase);
        Assert.AreEqual(bossStageManager.GetBossMaxLife() - 1, bossLife);

        for (int i = 0; i < bossStageManager.darkHearts.Length; i++)
        {
            bool expectedActive = i < bossLife;
            Assert.AreEqual(expectedActive, bossStageManager.darkHearts[i].activeSelf);
        }
    }

    [Test]
    public void TestPauseGame()
    {
        bossStageManager.PauseGame();

        Assert.IsTrue(bossStageManager.pauseMenu.activeSelf);
        Assert.AreEqual(0f, Time.timeScale);

        bool isGameOver = (bool)bossStageManager.GetType().BaseType.GetField("isGameOver", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(bossStageManager);
        Assert.IsFalse(isGameOver);
    }

    [Test]
    public void TestResumeGame()
    {
        bossStageManager.PauseGame();
        bossStageManager.ResumeGame();

        Assert.AreEqual(1f, Time.timeScale);
    }

    [Test]
    public void TestActiveCharacterInitialization()
    {
        GameObject activeCharacter = (GameObject)bossStageManager.GetType().BaseType.GetField("activeCharacter", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(bossStageManager);
        Assert.IsNotNull(activeCharacter);

        var playerScript = activeCharacter.GetComponent<BossStagePlayer>();
        Assert.IsNotNull(playerScript);
    }

    [Test]
    public void TestUpdateHeartsUI()
    {
        gameManager.RemoveLife();
        gameManager.RemoveLife();

        InvokeNonPublicMethod(bossStageManager, "UpdateHeartsUI");

        for (int i = 0; i < bossStageManager.hearts.Length; i++)
        {
            bool expectedActive = i < gameManager.GetLife();
            Assert.AreEqual(expectedActive, bossStageManager.hearts[i].activeSelf);
        }
    }
}
