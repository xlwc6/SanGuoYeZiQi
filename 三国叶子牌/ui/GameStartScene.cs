using Godot;
using System;
using System.Threading.Tasks;

public partial class GameStartScene : Control
{
    public bool GameStarted = false;

    private Button startBtn;
    private TextureRect enemyAvatar;
    private Label enemyName;
    private PanelContainer enemyPanel;
    private Label enemyLineLabel;
    private Timer enemyTypewriter;
    private TextureRect playerAvatar;
    private Label playerName;
    private PanelContainer playerPanel;
    private Label playerLineLabel;
    private Timer playerTypewriter;

    private string avatar_folder = "res://accests/avatars/";
    private string lines_path = "res://resources/others/character_lines.txt";
    private Godot.Collections.Array<string> character_lines = [];
    private Godot.Collections.Array<string> player_lines = [];
    private bool is_reversal = false; // 反转对话的标识

    public override void _Ready()
    {
        startBtn = GetNode<Button>("%开始游戏");
        enemyAvatar = GetNode<TextureRect>("%敌方图片");
        enemyName = GetNode<Label>("%敌方昵称");
        enemyPanel = GetNode<PanelContainer>("%敌方台词框");
        enemyLineLabel = GetNode<Label>("%敌方台词");
        enemyTypewriter = GetNode<Timer>("%敌方打字机");
        playerAvatar = GetNode<TextureRect>("%玩家图片");
        playerName = GetNode<Label>("%玩家昵称");
        playerPanel = GetNode<PanelContainer>("%玩家台词框");
        playerLineLabel = GetNode<Label>("%玩家台词");
        playerTypewriter = GetNode<Timer>("%玩家打字机");

        startBtn.Pressed += StartBtn_Pressed;
        enemyTypewriter.Timeout += EnemyTypewriter_Timeout;
        playerTypewriter.Timeout += PlayerTypewriter_Timeout;

        LoadLines();

        GetNode<SoundManager>("/root/SoundManager").SetupUISounds(startBtn);
    }

    private void StartBtn_Pressed()
    {
        // 先显示敌方台词，然后结束显示我方的，最后隐藏界面
        ShowLines(character_lines.PickRandom(), OwnerType.Enemy);
    }

    private void EnemyTypewriter_Timeout()
    {
        var char_count = enemyLineLabel.Text.Length;
        if (enemyLineLabel.VisibleCharacters < char_count)
        {
            enemyLineLabel.VisibleCharacters += 1;

        }
        else
        {
            enemyTypewriter.Stop();

            if (is_reversal)
            {
                GameOver(true);
            }
            else
            {
                ShowLines(player_lines.PickRandom(), OwnerType.Player);
            }
        }
    }

    private void PlayerTypewriter_Timeout()
    {
        var char_count = playerLineLabel.Text.Length;
        if (playerLineLabel.VisibleCharacters < char_count)
        {
            playerLineLabel.VisibleCharacters += 1;

        }
        else
        {
            playerTypewriter.Stop();

            if (playerLineLabel.Text == "此人表面张扬，内里空虚，根本不配做对手！")
            {
                // 抽到这个，对面会心态奔溃，然后胜利
                is_reversal = true;
                ShowLines("你...你...尼玛，噗", OwnerType.Enemy);

            }
            else if (playerLineLabel.Text == "此人中二入骨，自命不凡，与其计较，反降我格，避之为上。")
            {
                // 抽到这个，直接失败
                GameOver(false);
            }
            else
            {
                Next();
            }
        }
    }

    private void LoadLines()
    {
        // 人机角色台词
        if (FileAccess.FileExists(lines_path))
        {
            using var file = FileAccess.Open(lines_path, FileAccess.ModeFlags.Read);
            if (file != null)
            {
                while (!file.EofReached())
                {
                    string line = file.GetLine().StripEscapes();
                    if (!string.IsNullOrEmpty(line))
                    {
                        character_lines.Add(line);
                    }
                }
            }
        }
        // 玩家角色台词
        player_lines.Add("此人看似行事鲁莽，实则心思缜密，不可小觑！");
        player_lines.Add("此人表面玩世不恭，暗藏雄才大略，切勿掉以轻心！");
        player_lines.Add("此人言语谦和，却锋芒内敛，绝非等闲之辈！");
        player_lines.Add("此人不动声色，却步步为营，需得小心提防！");
        player_lines.Add("此人外表温和，内心刚烈，不可不防！");
        player_lines.Add("此人看似勇猛，实则草包一个，不足挂齿！");
        player_lines.Add("此人行事鲁莽，毫无章法，简直不值一提！");
        player_lines.Add("此人平庸无奇，既无谋略也无胆识，无须放在心上！");
        player_lines.Add("此人表面张扬，内里空虚，根本不配做对手！"); 
        player_lines.Add("此人中二入骨，自命不凡，与其计较，反降我格，避之为上。"); 
    }

    private void ShowLines(string text,OwnerType owner)
    {
        if (owner == OwnerType.Enemy)
        {
            enemyLineLabel.Text = text;
            enemyLineLabel.VisibleCharacters = 0;
            enemyPanel.Show();
            enemyTypewriter.Start();
        }
        else
        {
            playerLineLabel.Text = text;
            playerLineLabel.VisibleCharacters = 0;
            playerPanel.Show();
            playerTypewriter.Start();
        }
    }

    private async void Next()
    {
        await Task.Delay(1000);

        GameStarted = true;
        Hide();
    }

    private async void GameOver(bool isWin)
    {
        await Task.Delay(1000);

        GameStarted = false;
        Hide();
        
        int ownerType = isWin ? (int)OwnerType.Enemy : (int)OwnerType.Player;
        GetNode<GameEvent>("/root/GameEvent").EmitSignal(GameEvent.SignalName.Death, ownerType);
    }

    public void ShowStart(PlayerInfo enemyInfo, PlayerInfo playerInfo)
    {
        // 初始化界面
        enemyPanel.Hide();
        playerPanel.Hide();
        // 加载头像等
        enemyAvatar.Texture = GD.Load<Texture2D>(avatar_folder + enemyInfo.Avatar);
        enemyName.Text = enemyInfo.Name;
        playerAvatar.Texture = GD.Load<Texture2D>(avatar_folder + playerInfo.Avatar);
        playerName.Text = playerInfo.Name;
        // 显示界面
        Show();
    }
}
