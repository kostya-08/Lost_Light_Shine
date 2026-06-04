using Godot;
using System;

public partial class Teleport : Area2D
{
	[Export] public Vector2 TargetPosition = new Vector2(50, 60);
	[Export] public string RequiredKey = "Ключ";
	[Export] public float FadeDuration = 0.5f;
	
	private bool _isTeleporting = false;
	private Player _player;
	private Tween _tween;
	private Label _messageLabel;
	private Sprite2D _portalSprite;
	private bool _isActive = false;
	private Key _key; // Ссылка на ключ
	
	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
		
		// Находим ключ на сцене
		_key = GetNodeOrNull<Key>("../Key");
		if (_key == null)
		{
			_key = GetNodeOrNull<Key>("/root/World/Key");
		}
		
		// Создаем сообщение
		_messageLabel = new Label();
		_messageLabel.Text = $"🔒 Нужен {RequiredKey}!";
		_messageLabel.AddThemeFontSizeOverride("font_size", 16);
		_messageLabel.Modulate = new Color(1, 0, 0, 0);
		_messageLabel.Visible = false;
		AddChild(_messageLabel);
		
		_portalSprite = GetNodeOrNull<Sprite2D>("Sprite2D");
		SetPortalActive(false);
		
		GD.Print($"Портал создан. Требуется ключ: {RequiredKey}");
	}
	
	public void ActivatePortal()
	{
		_isActive = true;
		SetPortalActive(true);
		GD.Print("Портал активирован!");
	}
	
	private void SetPortalActive(bool active)
	{
		if (_portalSprite != null)
		{
			if (active)
			{
				_portalSprite.Modulate = new Color(0, 1, 0, 1);
			}
			else
			{
				_portalSprite.Modulate = new Color(1, 0, 0, 0.5f);
			}
		}
	}
	
	private async void OnBodyEntered(Node2D body)
	{
		if (body is Player player && !_isTeleporting)
		{
			_player = player;
			
			if (!_isActive)
			{
				await ShowMessage("🔒 Нужен " + RequiredKey + "!");
				return;
			}
			
			_isTeleporting = true;
			
			// Забираем ключ у игрока
			player.UseKey(RequiredKey);
			
			// Запускаем респавн ключа
			if (_key != null)
			{
				_key.Respawn();
			}
			
			// Отключаем управление
			player.SetProcess(false);
			player.SetPhysicsProcess(false);
			
			await FadeOut();
			
			// Телепортируем
			player.Position = TargetPosition;
			
			await FadeIn();
			
			// Включаем управление
			player.SetProcess(true);
			player.SetPhysicsProcess(true);
			
			// Деактивируем портал до следующего ключа
			_isActive = false;
			SetPortalActive(false);
			
			_isTeleporting = false;
		}
	}
	
	private async System.Threading.Tasks.Task ShowMessage(string text)
	{
		if (_messageLabel == null) return;
		
		_messageLabel.Text = text;
		_messageLabel.Position = new Vector2(-50, -60);
		_messageLabel.Modulate = new Color(1, 0, 0, 1);
		_messageLabel.Visible = true;
		
		await ToSignal(GetTree().CreateTimer(1.0f), SceneTreeTimer.SignalName.Timeout);
		
		var tween = CreateTween();
		tween.TweenProperty(_messageLabel, "modulate", new Color(1, 0, 0, 0), 0.3f);
		await ToSignal(tween, Tween.SignalName.Finished);
		
		_messageLabel.Visible = false;
	}
	
	private async System.Threading.Tasks.Task FadeOut()
	{
		CanvasItem visual = GetPlayerVisual();
		if (visual == null) return;
		
		_tween = CreateTween();
		_tween.TweenProperty(visual, "modulate", new Color(1, 1, 1, 0), FadeDuration);
		await ToSignal(_tween, Tween.SignalName.Finished);
	}
	
	private async System.Threading.Tasks.Task FadeIn()
	{
		CanvasItem visual = GetPlayerVisual();
		if (visual == null) return;
		
		_tween = CreateTween();
		_tween.TweenProperty(visual, "modulate", new Color(1, 1, 1, 1), FadeDuration);
		await ToSignal(_tween, Tween.SignalName.Finished);
	}
	
	private CanvasItem GetPlayerVisual()
	{
		if (_player.HasNode("AnimatedSprite2D"))
			return _player.GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		if (_player.HasNode("Sprite2D"))
			return _player.GetNode<Sprite2D>("Sprite2D");
		return _player;
	}
}
