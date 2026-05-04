using Godot;
using System;

public partial class Key : Area2D
{
	[Export] public string KeyName = "Ключ";
	[Export] public float RespawnTime = 3.0f; // Время до появления ключа после телепорта
	
	private bool _isCollected = false;
	private AnimatedSprite2D _animatedSprite;
	private CollisionShape2D _collisionShape;
	private Vector2 _startPosition;
	private bool _isRespawning = false;
	
	public override void _Ready()
	{
		_animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
		_startPosition = Position;
		
		BodyEntered += OnBodyEntered;
		
		GD.Print($"Ключ {KeyName} готов на позиции {_startPosition}");
	}
	
	private void OnBodyEntered(Node2D body)
	{
		if (_isCollected) return;
		if (_isRespawning) return;
		
		if (body is Player player)
		{
			_isCollected = true;
			
			// Отключаем коллизию
			if (_collisionShape != null)
				_collisionShape.Disabled = true;
			
			// Добавляем ключ игроку
			player.CollectKey(KeyName);
			
			// Анимация подбора
			AnimatePickup();
		}
	}
	
	private async void AnimatePickup()
	{
		var tween = CreateTween();
		
		// Поднимаемся вверх и исчезаем
		tween.Parallel().TweenProperty(this, "position", _startPosition + new Vector2(0, -50), 0.5f);
		
		if (_animatedSprite != null)
		{
			tween.Parallel().TweenProperty(_animatedSprite, "modulate", new Color(1, 1, 1, 0), 0.5f);
		}
		
		await ToSignal(tween, Tween.SignalName.Finished);
		
		// Не удаляем ключ, а просто скрываем
		Visible = false;
	}
	
	public async void Respawn()
	{
		if (_isRespawning) return;
		
		_isRespawning = true;
		
		// Ждем время респавна
		await ToSignal(GetTree().CreateTimer(RespawnTime), SceneTreeTimer.SignalName.Timeout);
		
		// Возвращаем на исходную позицию
		Position = _startPosition;
		
		// Сбрасываем состояние
		_isCollected = false;
		Visible = true;
		
		if (_animatedSprite != null)
		{
			_animatedSprite.Modulate = new Color(1, 1, 1, 1);
		}
		
		if (_collisionShape != null)
		{
			_collisionShape.Disabled = false;
		}
		
		// Анимация появления
		var tween = CreateTween();
		tween.TweenProperty(this, "scale", new Vector2(0.5f, 0.5f), 0.1f);
		tween.TweenProperty(this, "scale", new Vector2(1f, 1f), 0.3f)
			 .SetTrans(Tween.TransitionType.Elastic);
		
		_isRespawning = false;
		
		GD.Print($"Ключ {KeyName} возродился!");
	}
}
