using Godot;
using System;

public partial class Chest : StaticBody2D
{
	[Export] public int BatteryCount = 1;
	
	private bool _isOpened = false;
	private bool _playerNearby = false;
	private TextureRect _interactIcon;
	private Player _player;
	private Sprite2D _sprite;
	private CollisionShape2D _collisionShape;
	private Tween _iconTween;
	private Area2D _detectionArea;
	
	public override void _Ready()
	{
		_sprite = GetNode<Sprite2D>("Sprite2D");
		_collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
		
		// Включаем коллизию для физического блока
		if (_collisionShape != null)
		{
			_collisionShape.Disabled = false;
		}
		
		// Создаем отдельную Area2D для обнаружения игрока
		_detectionArea = new Area2D();
		var detectionShape = new CollisionShape2D();
		var rect = new RectangleShape2D();
		rect.Size = new Vector2(40, 40);
		detectionShape.Shape = rect;
		_detectionArea.AddChild(detectionShape);
		AddChild(_detectionArea);
		
		// Настройка обнаружения
		_detectionArea.Monitoring = true;
		_detectionArea.Monitorable = true;
		_detectionArea.CollisionLayer = 1;
		_detectionArea.CollisionMask = 1;
		_detectionArea.BodyEntered += OnBodyEntered;
		_detectionArea.BodyExited += OnBodyExited;
		
		// Создаем иконку
		_interactIcon = new TextureRect();
		_interactIcon.Size = new Vector2(20, 20);
		_interactIcon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
		_interactIcon.Modulate = new Color(1, 1, 1, 0);
		_interactIcon.Visible = false;
		
		var eTexture = ResourceLoader.Load<Texture2D>("res://EButtonLabel.png");
		if (eTexture != null)
			_interactIcon.Texture = eTexture;
		
		AddChild(_interactIcon);
		
		// Z-index для отображения
		ZIndex = 2;
		
		GD.Print("Сундук готов!");
	}
	
	public override void _Process(double delta)
	{
		if (_interactIcon != null && _interactIcon.Visible)
		{
			_interactIcon.Position = new Vector2(-16, -40);
		}
		
		if (_playerNearby && !_isOpened && Input.IsActionJustPressed("interact"))
		{
			TakeReward();
		}
	}
	
	private void OnBodyEntered(Node2D body)
	{
		if (body is Player player && !_isOpened)
		{
			_player = player;
			_playerNearby = true;
			
			_interactIcon.Visible = true;
			
			if (_iconTween != null) _iconTween.Kill();
			_iconTween = CreateTween();
			_iconTween.TweenProperty(_interactIcon, "modulate", new Color(1, 1, 1, 1), 0.2f);
			
			GD.Print("Игрок подошел к сундуку");
		}
	}
	
	private void OnBodyExited(Node2D body)
	{
		if (body is Player)
		{
			_playerNearby = false;
			
			if (_iconTween != null) _iconTween.Kill();
			_iconTween = CreateTween();
			_iconTween.TweenProperty(_interactIcon, "modulate", new Color(1, 1, 1, 0), 0.2f);
			_iconTween.Finished += () => _interactIcon.Visible = false;
		}
	}
	
	private void TakeReward()
	{
		if (_player == null) return;
		
		_isOpened = true;
		_playerNearby = false;
		_interactIcon.Visible = false;
		
		_player.AddBattery(BatteryCount);
		
		// НЕ МЕНЯЕМ ЦВЕТ, НЕ ОТКЛЮЧАЕМ КОЛЛИЗИЮ
		// Сундук остается как был, просто больше нельзя открыть
		
		GD.Print($"Сундук открыт! +{BatteryCount} батарейка");
	}
}
