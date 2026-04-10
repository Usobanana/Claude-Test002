using UnityEngine;

/// <summary>
/// ゲーム全体の状態を管理するシングルトン
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState CurrentState { get; private set; } = GameState.Title;

    // 現在受注中のクエストID（0 = なし）
    public int ActiveQuestId { get; private set; }

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ChangeState(GameState newState)
    {
        CurrentState = newState;
        Debug.Log($"[GameManager] State: {newState}");
    }

    public void AcceptQuest(int questId)
    {
        ActiveQuestId = questId;
        Debug.Log($"[GameManager] Quest accepted: {questId}");
    }

    public void ClearQuest()
    {
        ActiveQuestId = 0;
    }

    // --- シーン遷移ショートカット ---

    public void GoToTitle() => Transition(SceneName.TitleScene, GameState.Title);
    public void GoToBase()  => Transition(SceneName.BaseScene,  GameState.Base);
    public void GoToField() => Transition(SceneName.FieldScene, GameState.Field);
    public void GoToResult()=> Transition(SceneName.ResultScene, GameState.Result);

    private void Transition(SceneName scene, GameState state)
    {
        ChangeState(state);
        SceneLoader.Instance.LoadScene(scene);
    }
}

public enum GameState
{
    Title,
    Base,
    Field,
    Result,
}
