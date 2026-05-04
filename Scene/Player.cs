using Godot;
using System;
using System.Collections.Generic;

public partial class Player : CharacterBody2D
{
	[Export] private float WalkSpeed = 200;
	[Export] private float RunSpeed = 300;
	[Export] private float Acceleration = 10.0f;
	[Export] private float FlashlightRotationSpeed = 5.0f;
	
	private AnimatedSprite2D _animatedSprite;
	private string _currentAnimation = "Idel_down";
	private Vector2 _lastDirection = Vector2.Down;
	
	private Node2D _flashlightPivot;
	private PointLight2D _flashlight;
	private float _targetRotation = 0;
	private float _currentSpeed = 200;
	
	// Инвентарь
	private Dictionary<string, int> _inventory = new Dictionary<string, int>();
	private CanvasLayer _uiLayer;
	private Label _keyLabel;
	private TextureRect _keyIcon;
	
	// Ссылка на портал
	private Teleport _portal;
	
	// Система здоровья
[Export] private int MaxHealth = 4;
[Export] private float HealthRegenDelay = 15.0f;

private int _currentHealth;
private bool _isDead = false;
private Timer _healthRegenDelayTimer;
private Timer _healthRegenTimer;
private CanvasLayer _damageEffect;
private ColorRect _damageOverlay;
private Tween _heartbeatTween;
private bool _isInjured = false;

// Сигнал смерти
[Signal]
public delegate void PlayerDiedEventHandler();
	
	public override void _Ready()
	{
		base._Ready();
		
		_animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_flashlightPivot = GetNode<Node2D>("FlashlightPivot");
		_flashlight = GetNode<PointLight2D>("FlashlightPivot/PointLight2D");
		
		if (_animatedSprite != null)
		{
			_animatedSprite.Stop();
			_animatedSprite.Play("Idel_down");
		}
		
		if (_flashlight != null)
		{
			_flashlight.Enabled = true;
		}
		
		_currentSpeed = WalkSpeed;
		
		// Создаем UI
		CreateUI();
		
		// Ищем портал на сцене
		_portal = GetNodeOrNull<Teleport>("../Teleport");
		if (_portal == null)
		{
			_portal = GetNodeOrNull<Teleport>("/root/World/Teleport");
		}
		
		GD.Print("Player готов!");
		
		// Инициализируем систему здоровья
		InitHealthSystem();
	}
	
	private void CreateUI()
{
	_uiLayer = new CanvasLayer();
	_uiLayer.Name = "PlayerUI";
	_uiLayer.Layer = 100;
	AddChild(_uiLayer);
	
	// Создаем просто иконку (без панели и цифр)
	_keyIcon = new TextureRect();
	_keyIcon.Size = new Vector2(64, 64); // Большой размер
	_keyIcon.Position = new Vector2(15, 15); // Отступ от края
	_keyIcon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
	
	// Загружаем вашу иконку ключа
	var texture = ResourceLoader.Load<Texture2D>("res://key3anim.png");
	if (texture != null)
	{
		_keyIcon.Texture = texture;
		GD.Print("Иконка ключа загружена");
	}
	else
	{
		// Если не загрузилась, создаем желтый квадрат
		GD.PrintErr("Не удалось загрузить keyanim.png, создаю заглушку");
		var image = Image.Create(64, 64, false, Image.Format.Rgba8);
		image.Fill(new Color(1, 0.8f, 0, 1));
		_keyIcon.Texture = ImageTexture.CreateFromImage(image);
	}
	
	// Иконка скрыта по умолчанию
	_keyIcon.Visible = false;
	
	_uiLayer.AddChild(_keyIcon);
}
	
	public void CollectKey(string keyName)
{
	if (_inventory.ContainsKey(keyName))
		_inventory[keyName]++;
	else
		_inventory[keyName] = 1;
	
	// Показываем иконку
	if (_keyIcon != null)
	{
		_keyIcon.Visible = true;
		
		// Анимация появления (увеличение)
		_keyIcon.Scale = new Vector2(0.5f, 0.5f);
		var tween = CreateTween();
		tween.TweenProperty(_keyIcon, "scale", new Vector2(1f, 1f), 0.3f)
			 .SetTrans(Tween.TransitionType.Elastic);
		
		// Анимация пульсации
		tween.TweenProperty(_keyIcon, "scale", new Vector2(1.1f, 1.1f), 0.2f);
		tween.TweenProperty(_keyIcon, "scale", new Vector2(1f, 1f), 0.2f);
	}
	
	ShowPickupMessage($"+ {keyName}!");
	
	// Активируем портал
	if (_portal != null)
	{
		_portal.ActivatePortal();
	}
	
	GD.Print($"Собран {keyName}!");
}
	
	public bool HasKey(string keyName)
	{
		return _inventory.ContainsKey(keyName) && _inventory[keyName] > 0;
	}
	
	public bool UseKey(string keyName)
{
	if (HasKey(keyName))
	{
		_inventory[keyName]--;
		
		if (_inventory[keyName] <= 0)
		{
			_inventory.Remove(keyName);
			// Скрываем иконку если ключей больше нет
			if (_keyIcon != null)
			{
				_keyIcon.Visible = false;
			}
		}
		
		UpdateUI();
		return true;
	}
	return false;
}
	
	private void UpdateUI()
	{
		if (_keyLabel == null) return;
		
		int totalKeys = 0;
		foreach (var count in _inventory.Values)
			totalKeys += count;
		
		_keyLabel.Text = totalKeys.ToString();
		
		// Анимация
		if (_keyIcon != null)
		{
			var tween = CreateTween();
			tween.TweenProperty(_keyIcon, "scale", new Vector2(1.3f, 1.3f), 0.1f);
			tween.TweenProperty(_keyIcon, "scale", new Vector2(1f, 1f), 0.1f);
		}
	}
	
	private void ShowPickupMessage(string message)
	{
		var label = new Label();
		label.Text = message;
		label.Position = new Vector2(-30, -60);
		label.AddThemeFontSizeOverride("font_size", 16);
		label.Modulate = new Color(1, 0.8f, 0, 1);
		
		_uiLayer.AddChild(label);
		
		var tween = CreateTween();
		tween.TweenProperty(label, "position", label.Position - new Vector2(0, 40), 1f);
		tween.TweenProperty(label, "modulate", new Color(1, 0.8f, 0, 0), 0.5f);
		tween.Finished += () => label.QueueFree();
	}
	
	public override void _Process(double delta)
	{
		Vector2 movement = MovementVector();
		Vector2 direction = movement.Normalized();
		
		float targetSpeed = WalkSpeed;
		bool isRunning = Input.GetActionStrength("run") > 0 && movement.Length() > 0;
		
		if (isRunning)
		{
			targetSpeed = RunSpeed;
		}
		
		_currentSpeed = Mathf.Lerp(_currentSpeed, targetSpeed, (float)delta * Acceleration);
		Velocity = _currentSpeed * direction;
		MoveAndSlide();
		
		UpdateAnimation(movement.Length() > 0);
		UpdateFlashlight(direction, movement.Length() > 0);
	}
	
	private void UpdateAnimation(bool isMoving)
	{
		if (_animatedSprite == null) return;
		
		string newAnimation = _currentAnimation;
		
		bool isMovingUp = Input.GetActionStrength("move_up") > 0;
		bool isMovingDown = Input.GetActionStrength("move_down") > 0;
		bool isMovingRight = Input.GetActionStrength("move_right") > 0;
		bool isMovingLeft = Input.GetActionStrength("move_left") > 0;
		
		if (isMoving)
		{
			if (isMovingUp)
			{
				newAnimation = "Run_up";
				_lastDirection = Vector2.Up;
			}
			else if (isMovingDown)
			{
				newAnimation = "Run_down";
				_lastDirection = Vector2.Down;
			}
			else if (isMovingRight)
			{
				newAnimation = "Run_right";
				_lastDirection = Vector2.Right;
			}
			else if (isMovingLeft)
			{
				newAnimation = "Run_left";
				_lastDirection = Vector2.Left;
			}
		}
		else
		{
			if (_lastDirection == Vector2.Up)
				newAnimation = "Idel_up";
			else if (_lastDirection == Vector2.Down)
				newAnimation = "Idel_down";
			else if (_lastDirection == Vector2.Right)
				newAnimation = "Idel_right";
			else if (_lastDirection == Vector2.Left)
				newAnimation = "Idel_left";
		}
		
		if (newAnimation != _currentAnimation)
		{
			_currentAnimation = newAnimation;
			_animatedSprite.Play(_currentAnimation);
		}
	}
	
	private void UpdateFlashlight(Vector2 direction, bool isMoving)
	{
		if (_flashlightPivot == null) return;
		
		Vector2 targetDirection;
		
		if (isMoving && direction != Vector2.Zero)
		{
			targetDirection = direction;
		}
		else
		{
			targetDirection = _lastDirection;
		}
		
		float targetAngle = targetDirection.Angle() - Mathf.Pi / 2;
		
		if (FlashlightRotationSpeed > 0)
		{
			_targetRotation = Mathf.LerpAngle(_targetRotation, targetAngle, (float)GetProcessDeltaTime() * FlashlightRotationSpeed);
			_flashlightPivot.Rotation = _targetRotation;
		}
		else
		{
			_flashlightPivot.Rotation = targetAngle;
			_targetRotation = targetAngle;
		}
	}
	
	private Vector2 MovementVector()
	{
		float movementX = Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left");
		float movementY = Input.GetActionStrength("move_down") - Input.GetActionStrength("move_up");
		return new Vector2(movementX, movementY);
	}
	
	// Инициализация системы здоровья (вызовите в _Ready)
private void InitHealthSystem()
{
	_currentHealth = MaxHealth;
	_isDead = false;
	
	GD.Print($"Здоровье: {_currentHealth}/{MaxHealth}");
	
	CreateDamageEffect();
	
	_healthRegenDelayTimer = new Timer();
	_healthRegenDelayTimer.WaitTime = HealthRegenDelay;
	_healthRegenDelayTimer.OneShot = true;
	_healthRegenDelayTimer.Timeout += StartHealthRegen;
	AddChild(_healthRegenDelayTimer);
	
	_healthRegenTimer = new Timer();
	_healthRegenTimer.WaitTime = 1.0f;
	_healthRegenTimer.Timeout += RegenerateHealth;
	AddChild(_healthRegenTimer);
}

private void CreateDamageEffect()
{
	_damageEffect = new CanvasLayer();
	_damageEffect.Layer = 200;
	AddChild(_damageEffect);
	
	_damageOverlay = new ColorRect();
	_damageOverlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
	_damageOverlay.Color = new Color(1, 0, 0, 0);
	_damageOverlay.MouseFilter = Control.MouseFilterEnum.Ignore;
	_damageEffect.AddChild(_damageOverlay);
}

public void TakeDamage(int damage)
{
	if (_isDead) return;
	
	GD.Print($"Урон! Здоровье было: {_currentHealth}");
	
	// Останавливаем регенерацию
	_healthRegenDelayTimer.Stop();
	_healthRegenTimer.Stop();
	
	_currentHealth -= damage;
	
	// Останавливаем текущую пульсацию
	if (_heartbeatTween != null && _heartbeatTween.IsRunning())
	{
		_heartbeatTween.Kill();
	}
	
	ShowDamageEffect();
	StartHeartbeat();
	
	_healthRegenDelayTimer.Start();
	
	GD.Print($"Здоровье стало: {_currentHealth}/{MaxHealth}");
	
	if (_currentHealth <= 0)
	{
		Die();
	}
}

private void ShowDamageEffect()
{
	// Останавливаем предыдущую анимацию
	if (_heartbeatTween != null && _heartbeatTween.IsRunning())
	{
		_heartbeatTween.Kill();
	}
	
	// Рассчитываем яркость в зависимости от здоровья
	float intensity;
	
	if (_currentHealth == 3)
	{
		intensity = 0.05f;  // Еле заметно
	}
	else if (_currentHealth == 2)
	{
		intensity = 0.12f;  // Слабо заметно
	}
	else if (_currentHealth == 1)
	{
		intensity = 0.25f;  // Хорошо заметно
	}
	else
	{
		intensity = 0.4f;   // Сильно заметно (перед смертью)
	}
	
	_damageOverlay.Color = new Color(1, 0, 0, intensity);
	_isInjured = true;
}

private void StartHeartbeat()
{
	// Останавливаем предыдущую пульсацию
	if (_heartbeatTween != null && _heartbeatTween.IsRunning())
	{
		_heartbeatTween.Kill();
	}
	
	float baseIntensity = _damageOverlay.Color.A;
	
	// Не запускаем пульсацию если интенсивность 0
	if (baseIntensity <= 0.01f)
	{
		return;
	}
	
	_heartbeatTween = CreateTween();
	_heartbeatTween.SetLoops();
	
	_heartbeatTween.TweenProperty(_damageOverlay, "color", new Color(1, 0, 0, baseIntensity + 0.05f), 0.2f);
	_heartbeatTween.TweenProperty(_damageOverlay, "color", new Color(1, 0, 0, baseIntensity - 0.03f), 0.4f);
	_heartbeatTween.TweenProperty(_damageOverlay, "color", new Color(1, 0, 0, baseIntensity + 0.05f), 0.2f);
	_heartbeatTween.TweenProperty(_damageOverlay, "color", new Color(1, 0, 0, baseIntensity - 0.03f), 0.4f);
}

private void StopHeartbeat()
{
	if (_heartbeatTween != null && _heartbeatTween.IsRunning())
	{
		_heartbeatTween.Kill();
	}
	
	// Плавно убираем красный цвет
	var fadeTween = CreateTween();
	fadeTween.TweenProperty(_damageOverlay, "color", new Color(1, 0, 0, 0), 0.5f);
	
	_isInjured = false;
}

private void StartHealthRegen()
{
	if (_isDead) return;
	if (_currentHealth >= MaxHealth) 
	{
		StopHeartbeat();
		return;
	}
	
	GD.Print("Начинаю регенерацию...");
	
	// Принудительно останавливаем пульсацию
	if (_heartbeatTween != null && _heartbeatTween.IsRunning())
	{
		_heartbeatTween.Kill();
	}
	
	_healthRegenTimer.Start();
}


private void RegenerateHealth()
{
	if (_isDead) return;
	
	// ЕСЛИ ЗДОРОВЬЕ УЖЕ ПОЛНОСТЬЮ ВОССТАНОВЛЕНО
	if (_currentHealth >= MaxHealth)
	{
		_healthRegenTimer.Stop();
		StopHeartbeat();  // ОБЯЗАТЕЛЬНО останавливаем пульсацию
		GD.Print("Здоровье полностью восстановлено! Эффект выключен.");
		return;
	}
	
	_currentHealth++;
	GD.Print($"Регенерация: {_currentHealth}/{MaxHealth}");
	
	// Останавливаем текущую пульсацию
	if (_heartbeatTween != null && _heartbeatTween.IsRunning())
	{
		_heartbeatTween.Kill();
	}
	
	// Если здоровье еще не полное, обновляем яркость
	if (_currentHealth < MaxHealth)
	{
		float newIntensity;
		
		if (_currentHealth == 2)
		{
			newIntensity = 0.12f;
		}
		else if (_currentHealth == 1)
		{
			newIntensity = 0.25f;
		}
		else
		{
			newIntensity = 0.05f;
		}
		
		_damageOverlay.Color = new Color(1, 0, 0, newIntensity);
		
		// Запускаем новую пульсацию с новой яркостью
		StartHeartbeat();
	}
	else
	{
		// Если здоровье стало полным - выключаем всё
		StopHeartbeat();
	}
}

private async void Die()
{
	_isDead = true;
	GD.Print("ИГРОК УМЕР!");
	
	StopHeartbeat();
	
	// Отключаем управление
	SetProcess(false);
	SetPhysicsProcess(false);
	
	// Эффект смерти
	var deathTween = CreateTween();
	deathTween.TweenProperty(_damageOverlay, "color", new Color(1, 0, 0, 0.6f), 0.3f);
	await ToSignal(deathTween, Tween.SignalName.Finished);
	
	// Телепортируем
	Respawn();
	
	// Эффект появления
	var respawnTween = CreateTween();
	respawnTween.TweenProperty(_damageOverlay, "color", new Color(1, 0, 0, 0), 0.5f);
	await ToSignal(respawnTween, Tween.SignalName.Finished);
	
	// Включаем управление
	SetProcess(true);
	SetPhysicsProcess(true);
}

private void Respawn()
{
	var teleport = GetNodeOrNull<Teleport>("/root/World/Teleport");
	if (teleport != null)
	{
		Position = teleport.TargetPosition;
	}
	else
	{
		Position = new Vector2(500, 300);
	}
	
	_currentHealth = MaxHealth;
	_isDead = false;
	_isInjured = false;
	_damageOverlay.Color = new Color(1, 0, 0, 0);
	
	_healthRegenDelayTimer.Stop();
	_healthRegenTimer.Stop();
	
	GD.Print($"Игрок воскрес! Здоровье: {_currentHealth}/{MaxHealth}");
}
}
