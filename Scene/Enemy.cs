using Godot;
using System;

public partial class Enemy : CharacterBody2D
{
	// Настройки движения
	[Export] private float WalkSpeed = 100.0f;
	[Export] private float ChaseSpeed = 130.0f;
	[Export] private float Acceleration = 8.0f;
	
	// Настройки зрения
	[Export] private float VisionRadius = 200.0f;
	[Export] private float ChaseTime = 5.0f;
	[Export] private float WanderChangeTime = 2.0f;
	
	// Настройки атаки
	[Export] private float AttackRange = 35.0f;
	[Export] private float AttackCooldown = 1.0f;
	[Export] private int Damage = 1;
	
	// Настройки отхода
	[Export] private float TooCloseDistance = 30.0f;  // Дистанция для отхода
	[Export] private float BackoffDistance = 40.0f;   // На сколько отходить
	[Export] private float BackoffSpeed = 80.0f;      // Скорость отхода
	
	// Компоненты
	private AnimatedSprite2D _animatedSprite;
	private Area2D _visionArea;
	private Area2D _attackArea;
	private Timer _wanderTimer;
	private Timer _chaseTimer;
	private Timer _attackTimer;
	private Timer _idleTimer;
	
	// Состояния
	private enum State { Wander, Chase, Attack, Backoff }
	private State _currentState = State.Wander;
	
	// Переменные
	private CharacterBody2D _player;
	private Vector2 _wanderDirection;
	private Vector2 _currentVelocity;
	private Vector2 _lastDirection = Vector2.Down;
	private bool _isAttacking = false;
	private bool _canAttack = true;
	private bool _playerInVision = false;
	private string _currentAnimation = "";
	
	// Переменные для отхода
	private Vector2 _backoffStartPosition;
	private bool _isBackingOff = false;
	
	public override void _Ready()
	{
		_animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		
		// Находим Area2D для зрения
		_visionArea = GetNode<Area2D>("VisionArea");
		if (_visionArea != null)
		{
			_visionArea.BodyEntered += OnPlayerEnteredVision;
			_visionArea.BodyExited += OnPlayerExitedVision;
		}
		
		// Находим Area2D для атаки
		_attackArea = GetNode<Area2D>("AttackArea");
		if (_attackArea != null)
		{
			_attackArea.BodyEntered += OnPlayerEnteredAttack;
		}
		
		// Таймеры
		_wanderTimer = new Timer();
		_wanderTimer.WaitTime = WanderChangeTime;
		_wanderTimer.Timeout += OnWanderTimeout;
		AddChild(_wanderTimer);
		_wanderTimer.Start();
		
		_chaseTimer = new Timer();
		_chaseTimer.WaitTime = ChaseTime;
		_chaseTimer.Timeout += OnChaseTimeout;
		AddChild(_chaseTimer);
		
		_attackTimer = new Timer();
		_attackTimer.WaitTime = AttackCooldown;
		_attackTimer.Timeout += () => 
		{ 
			_canAttack = true;
			// После перезарядки атаки возвращаемся к преследованию
			if (_currentState == State.Attack && !_isAttacking)
			{
				_currentState = State.Chase;
			}
		};
		AddChild(_attackTimer);
		
		// Таймер для Idle анимации после атаки
		_idleTimer = new Timer();
		_idleTimer.WaitTime = 0.5f;
		_idleTimer.Timeout += () => 
		{
			if (!_isAttacking && _currentVelocity.Length() < 5f)
			{
				_currentAnimation = "";
				UpdateAnimation();
			}
		};
		AddChild(_idleTimer);
		
		// Поиск игрока
		_player = GetNodeOrNull<CharacterBody2D>("/root/World/Player");
		
		// Случайное начальное направление
		float startAngle = (float)GD.RandRange(0, Mathf.Pi * 2);
		_wanderDirection = new Vector2(Mathf.Cos(startAngle), Mathf.Sin(startAngle));
		
		// Запускаем начальную анимацию
		_lastDirection = Vector2.Down;
		_animatedSprite.Play("idel_down");
		_currentAnimation = "idel_down";
		
		GD.Print("Враг готов!");
	}
	
	private void OnPlayerEnteredVision(Node2D body)
	{
		if (body is CharacterBody2D player && player.Name == "Player")
		{
			_playerInVision = true;
		}
	}
	
	private void OnPlayerExitedVision(Node2D body)
	{
		if (body is CharacterBody2D player && player.Name == "Player")
		{
			_playerInVision = false;
		}
	}
	
	private void OnPlayerEnteredAttack(Node2D body)
	{
		if (body is CharacterBody2D player && player.Name == "Player")
		{
			if (_currentState == State.Chase && _canAttack && !_isAttacking && !_isBackingOff)
			{
				_currentState = State.Attack;
				_isAttacking = true;
				_canAttack = false;
				_attackTimer.Start();
				Attack(player);
			}
		}
	}
	
	public override void _PhysicsProcess(double delta)
	{
		if (_player == null)
		{
			_player = GetNodeOrNull<CharacterBody2D>("/root/World/Player");
			return;
		}
		
		UpdateState();
		UpdateMovement((float)delta);
		UpdateAnimation();
		UpdateSpriteFlip();
	}
	
	private void UpdateState()
	{
		if (_isAttacking) return;
		
		float distanceToPlayer = Position.DistanceTo(_player.Position);
		
		// Проверка на слишком близкое расстояние (ЗАЛИПАНИЕ)
		if (distanceToPlayer < TooCloseDistance && (_currentState == State.Chase || _currentState == State.Attack) && !_isBackingOff)
		{
			_currentState = State.Backoff;
			_backoffStartPosition = Position;
			_isBackingOff = true;
			
			// Определяем направление отхода (от игрока)
			Vector2 awayFromPlayer = (Position - _player.Position).Normalized();
			if (awayFromPlayer != Vector2.Zero)
			{
				_lastDirection = awayFromPlayer;
			}
			
			GD.Print("Враг: Слишком близко! Отхожу.");
			return;
		}
		
		switch (_currentState)
		{
			case State.Wander:
				if (_playerInVision)
				{
					_currentState = State.Chase;
					_chaseTimer.Start();
					_wanderTimer.Stop();
				}
				break;
				
			case State.Chase:
				if (!_playerInVision)
				{
					_chaseTimer.Start();
				}
				else
				{
					_chaseTimer.Start();
				}
				break;
				
			case State.Backoff:
				// Проверяем, закончился ли отход
				float backoffDistanceTraveled = Position.DistanceTo(_backoffStartPosition);
				if (backoffDistanceTraveled >= BackoffDistance)
				{
					_currentState = State.Chase;
					_isBackingOff = false;
					GD.Print("Враг: Отход закончен, продолжаю преследование.");
				}
				break;
		}
	}
	
	private void UpdateMovement(float delta)
	{
		Vector2 targetVelocity = Vector2.Zero;
		
		switch (_currentState)
		{
			case State.Wander:
				if (_wanderDirection != Vector2.Zero)
				{
					targetVelocity = _wanderDirection.Normalized() * WalkSpeed;
				}
				break;
				
			case State.Chase:
				if (_player != null && _playerInVision)
				{
					Vector2 toPlayer = _player.Position - Position;
					float distanceToPlayer = toPlayer.Length();
					
					// Нормальное преследование
					targetVelocity = toPlayer.Normalized() * ChaseSpeed;
					
					if (toPlayer.Length() > 0.1f)
					{
						_lastDirection = toPlayer.Normalized();
					}
				}
				else if (!_playerInVision && _chaseTimer.TimeLeft == 0 && _currentState == State.Chase)
				{
					_currentState = State.Wander;
					_wanderTimer.Start();
				}
				break;
				
			case State.Backoff:
				if (_player != null)
				{
					// Движемся ОТ игрока
					Vector2 awayFromPlayer = (Position - _player.Position).Normalized();
					targetVelocity = awayFromPlayer * BackoffSpeed;
					
					if (awayFromPlayer.Length() > 0.1f)
					{
						_lastDirection = awayFromPlayer;
					}
				}
				break;
				
			case State.Attack:
				targetVelocity = Vector2.Zero;
				break;
		}
		
		// Плавное изменение скорости
		_currentVelocity = _currentVelocity.Lerp(targetVelocity, Acceleration * delta);
		
		if (_currentVelocity.Length() < 5f)
		{
			_currentVelocity = Vector2.Zero;
		}
		
		Velocity = _currentVelocity;
		MoveAndSlide();
		
		if (_currentVelocity.Length() > 5f)
		{
			_lastDirection = _currentVelocity.Normalized();
			_idleTimer.Stop();
		}
		else
		{
			// Если стоим и не атакуем - запускаем таймер для Idle
			if (!_isAttacking && !_idleTimer.IsStopped() == false)
			{
				_idleTimer.Start();
			}
		}
	}
	
	private void UpdateSpriteFlip()
	{
		if (_animatedSprite == null) return;
		
		if (_lastDirection.X > 0)
		{
			_animatedSprite.FlipH = true;
		}
		else if (_lastDirection.X < 0)
		{
			_animatedSprite.FlipH = false;
		}
	}
	
	private void UpdateAnimation()
	{
		if (_animatedSprite == null) return;
		
		string newAnimation = "";
		bool isMoving = _currentVelocity.Length() > 5f;
		
		if (_isAttacking)
		{
			newAnimation = GetDirectionalAnimation("attack");
		}
		else if (isMoving)
		{
			newAnimation = GetDirectionalAnimation("walk");
			_idleTimer.Stop();
		}
		else
		{
			newAnimation = GetDirectionalAnimation("idel");
		}
		
		if (newAnimation != _currentAnimation && newAnimation != "")
		{
			_currentAnimation = newAnimation;
			_animatedSprite.Play(newAnimation);
		}
	}
	
	private string GetDirectionalAnimation(string baseName)
	{
		if (Mathf.Abs(_lastDirection.X) > Mathf.Abs(_lastDirection.Y))
		{
			return $"{baseName}_left";
		}
		else
		{
			if (_lastDirection.Y > 0)
				return $"{baseName}_down";
			else
				return $"{baseName}_up";
		}
	}
	
	private void OnWanderTimeout()
	{
		if (_currentState == State.Wander && !_isAttacking && !_isBackingOff)
		{
			float angle = (float)GD.RandRange(0, Mathf.Pi * 2);
			_wanderDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
			
			if (GD.Randf() < 0.3f)
			{
				_wanderDirection = Vector2.Zero;
			}
		}
	}
	
	private void OnChaseTimeout()
	{
		if (_currentState == State.Chase && !_playerInVision && !_isAttacking && !_isBackingOff)
		{
			_currentState = State.Wander;
			_wanderTimer.Start();
		}
	}
	
	private async void Attack(CharacterBody2D player)
	{
		GD.Print($"Враг атакует! Наносит {Damage} урона.");
		
		string attackAnim = GetDirectionalAnimation("attack");
		_animatedSprite.Play(attackAnim);
		_currentAnimation = attackAnim;
		
		await ToSignal(GetTree().CreateTimer(0.15f), SceneTreeTimer.SignalName.Timeout);
		
		// Нанесение урона
		 if (player.HasMethod("TakeDamage"))
		 {
			 player.Call("TakeDamage", Damage);
		 }
		
		await ToSignal(GetTree().CreateTimer(0.3f), SceneTreeTimer.SignalName.Timeout);
		
		_isAttacking = false;
		
		// После атаки проверяем дистанцию до игрока
		float distanceToPlayer = Position.DistanceTo(_player.Position);
		if (distanceToPlayer < TooCloseDistance)
		{
			_currentState = State.Backoff;
			_backoffStartPosition = Position;
			_isBackingOff = true;
			GD.Print("Враг: После атаки отхожу.");
		}
		else
		{
			_currentState = State.Chase;
		}
		
		// Запускаем таймер для Idle анимации
		_idleTimer.Start();
	}
}
