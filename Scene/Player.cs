using Godot;
using System;
using System.Collections.Generic;

public partial class Player : CharacterBody2D
{
	[Export] private float WalkSpeed = 150;
	[Export] private float RunSpeed = 200;
	[Export] private float Acceleration = 10.0f;
	[Export] private float FlashlightRotationSpeed = 5.0f;
	
	// ФОНАРИК - система затухания
	[Export] private float MaxBattery = 100f;
	[Export] private float BatteryDrainRate = 2f;
	private float _currentBattery = 100f;
	
	private AnimatedSprite2D _animatedSprite;
	private string _currentAnimation = "Idel_down";
	private Vector2 _lastDirection = Vector2.Down;
	
	private Node2D _flashlightPivot;
	private PointLight2D _flashlight;
	private float _targetRotation = 0;
	private float _currentSpeed = 200;
	
	// Инвентарь
	private int _batteryCount = 0;
	private int _keyCount = 0;
	
	// UI элементы
	private CanvasLayer _uiLayer;
	private Label _batteryLabel;
	private TextureRect _batteryIcon;
	private TextureRect _keyIcon;
	
	// Картинки для сообщений
	private Texture2D _plusKeyTexture;
	private Texture2D _plusBatteryTexture;
	private Texture2D _useBatteryTexture;
	private Texture2D _noBatteryTexture;
	
	// Ссылка на портал
	private Teleport _portal;
	
	// Система сообщений
	private Label _actionMessage;
	private bool _messageActive = false;
	
	// ==================== СИСТЕМА ЗДОРОВЬЯ ====================
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
		_currentBattery = MaxBattery;
		
		// Загружаем картинки для сообщений
		_plusKeyTexture = ResourceLoader.Load<Texture2D>("res://plus_key.png");
		_plusBatteryTexture = ResourceLoader.Load<Texture2D>("res://plus_battery.png");
		_useBatteryTexture = ResourceLoader.Load<Texture2D>("res://use_battery.png");
		_noBatteryTexture = ResourceLoader.Load<Texture2D>("res://no_battery.png");
		
		if (_plusKeyTexture == null)
			GD.PrintErr("Не найдена картинка plus_key.png");
		if (_plusBatteryTexture == null)
			GD.PrintErr("Не найдена картинка plus_battery.png");
		if (_useBatteryTexture == null)
			GD.PrintErr("Не найдена картинка use_battery.png");
		if (_noBatteryTexture == null)
			GD.PrintErr("Не найдена картинка no_battery.png");
		
		// Создаем UI
		CreateResourceUI();
		CreateActionMessage();
		
		// Инициализируем систему здоровья
		InitHealthSystem();
		
		// Ищем портал
		_portal = GetNodeOrNull<Teleport>("../Teleport");
		if (_portal == null)
		{
			_portal = GetNodeOrNull<Teleport>("/root/World/Teleport");
		}
		
		GD.Print("Player готов!");
	}
	
	private void CreateResourceUI()
	{
		_uiLayer = new CanvasLayer();
		_uiLayer.Name = "PlayerUI";
		_uiLayer.Layer = 100;
		AddChild(_uiLayer);
		
		// Панель без фона
		var panel = new Panel();
		panel.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
		panel.Position = new Vector2(10, 10);
		panel.Size = new Vector2(220, 110);
		
		var styleBox = new StyleBoxFlat();
		styleBox.BgColor = new Color(0, 0, 0, 0);
		panel.AddThemeStyleboxOverride("panel", styleBox);
		
		// ---- БАТАРЕЙКИ (слева) ----
		_batteryIcon = new TextureRect();
		_batteryIcon.Position = new Vector2(10, 5);
		_batteryIcon.Size = new Vector2(96, 96);
		_batteryIcon.ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional;
		_batteryIcon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
		
		var batteryTexture = ResourceLoader.Load<Texture2D>("res://Battery.png");
		if (batteryTexture != null)
		{
			_batteryIcon.Texture = batteryTexture;
		}
		else
		{
			GD.PrintErr("Не найдена текстура Battery.png");
			var image = Image.CreateEmpty(96, 96, false, Image.Format.Rgba8);
			image.Fill(new Color(1, 0.8f, 0, 1));
			_batteryIcon.Texture = ImageTexture.CreateFromImage(image);
		}
		
		_batteryLabel = new Label();
		_batteryLabel.Position = new Vector2(80, 35);
		_batteryLabel.Text = "";
		_batteryLabel.AddThemeFontSizeOverride("font_size", 36);
		_batteryLabel.Modulate = new Color(1, 1, 1, 1);
		
		// ---- КЛЮЧ (справа) ----
		_keyIcon = new TextureRect();
		_keyIcon.Position = new Vector2(120, 5);
		_keyIcon.Size = new Vector2(96, 96);
		_keyIcon.ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional;
		_keyIcon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
		
		var keyTexture = ResourceLoader.Load<Texture2D>("res://key3anim.png");
		if (keyTexture != null)
		{
			_keyIcon.Texture = keyTexture;
		}
		else
		{
			GD.PrintErr("Не найдена текстура ключа");
			var image = Image.CreateEmpty(96, 96, false, Image.Format.Rgba8);
			image.Fill(new Color(1, 0.5f, 0, 1));
			_keyIcon.Texture = ImageTexture.CreateFromImage(image);
		}
		
		panel.AddChild(_batteryIcon);
		panel.AddChild(_batteryLabel);
		panel.AddChild(_keyIcon);
		_uiLayer.AddChild(panel);
		
		// Изначально всё скрыто
		_batteryIcon.Visible = false;
		_batteryLabel.Visible = false;
		_keyIcon.Visible = false;
	}
	
	private void CreateActionMessage()
	{
		_actionMessage = new Label();
		_actionMessage.Name = "ActionMessage";
		_actionMessage.HorizontalAlignment = HorizontalAlignment.Center;
		_actionMessage.AddThemeFontSizeOverride("font_size", 24);
		_actionMessage.Modulate = new Color(1, 1, 1, 0);
		_actionMessage.SetAnchorsPreset(Control.LayoutPreset.BottomWide);
		_actionMessage.Position = new Vector2(0, -50);
		_uiLayer.AddChild(_actionMessage);
	}
	
	// ==================== ПОКАЗ КАРТИНОК ====================
	
	private void ShowPlusImage(string type, int count = 1)
	{
		Texture2D texture = null;
		
		if (type == "key")
			texture = _plusKeyTexture;
		else if (type == "battery")
			texture = _plusBatteryTexture;
		else if (type == "use_battery")
			texture = _useBatteryTexture;
		else if (type == "no_battery")
			texture = _noBatteryTexture;
		
		if (texture == null) return;
		
		// Удаляем старые сообщения
		foreach (Node child in _uiLayer.GetChildren())
		{
			if (child.Name == "PlusImage")
				child.QueueFree();
		}
		
		// Контейнер для картинки
		var container = new Control();
		container.Name = "PlusImage";
		container.SetAnchorsPreset(Control.LayoutPreset.BottomWide);
		container.Position = new Vector2(800, -260);
		container.Size = new Vector2(400, 400);
		_uiLayer.AddChild(container);
		
		// Картинка
		var picture = new TextureRect();
		picture.Texture = texture;
		picture.Size = new Vector2(384, 384);
		picture.Position = new Vector2(8, 8);
		picture.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
		picture.Modulate = new Color(1, 1, 1, 0);
		container.AddChild(picture);
		
		// Цифра количества (только для battery и key)
		Label countLabel = null;
		if (count > 1 && type != "use_battery" && type != "no_battery")
		{
			countLabel = new Label();
			countLabel.Text = $"x{count}";
			countLabel.Position = new Vector2(280, 260);
			countLabel.AddThemeFontSizeOverride("font_size", 72);
			countLabel.Modulate = new Color(1, 1, 1, 0);
			container.AddChild(countLabel);
		}
		
		// Анимация появления
		var appearTween = CreateTween();
		appearTween.TweenProperty(picture, "modulate", new Color(1, 1, 1, 1), 0.2f);
		if (countLabel != null)
			appearTween.Parallel().TweenProperty(countLabel, "modulate", new Color(1, 1, 1, 1), 0.2f);
		
		// Задержка перед исчезновением
		var timer = GetTree().CreateTimer(2.5f);
		timer.Timeout += () =>
		{
			var disappearTween = CreateTween();
			disappearTween.TweenProperty(picture, "modulate", new Color(1, 1, 1, 0), 0.3f);
			if (countLabel != null)
				disappearTween.Parallel().TweenProperty(countLabel, "modulate", new Color(1, 1, 1, 0), 0.3f);
			disappearTween.Finished += () => container.QueueFree();
		};
	}
	
	// ==================== РАБОТА С БАТАРЕЙКАМИ ====================
	
	public void AddBattery(int count = 1)
	{
		_batteryCount += count;
		
		_batteryIcon.Visible = true;
		_batteryLabel.Visible = true;
		
		_batteryIcon.Scale = new Vector2(0.5f, 0.5f);
		_batteryLabel.Scale = new Vector2(0.5f, 0.5f);
		var tween = CreateTween();
		tween.TweenProperty(_batteryIcon, "scale", new Vector2(1f, 1f), 0.3f)
			 .SetTrans(Tween.TransitionType.Elastic);
		tween.Parallel().TweenProperty(_batteryLabel, "scale", new Vector2(1f, 1f), 0.3f);
		
		UpdateBatteryUI();
		
		// Показываем картинку "+ Батарейка"
		ShowPlusImage("battery", count);
		
		GD.Print($"Добавлено {count} батареек. Всего: {_batteryCount}");
	}
	
	public bool UseBattery()
	{
		if (_batteryCount > 0)
		{
			_batteryCount--;
			UpdateBatteryUI();
			
			_currentBattery = Mathf.Min(_currentBattery + 50f, MaxBattery);
			
			if (!_flashlight.Enabled && _currentBattery > 0)
			{
				_flashlight.Enabled = true;
			}
			
			GD.Print($"Батарейка использована! Осталось: {_batteryCount}, Заряд: {_currentBattery}%");
			return true;
		}
		return false;
	}
	
	private void UpdateBatteryUI()
	{
		if (_batteryCount == 0)
		{
			_batteryIcon.Visible = false;
			_batteryLabel.Visible = false;
		}
		else
		{
			_batteryIcon.Visible = true;
			_batteryLabel.Visible = true;
			
			if (_batteryCount == 1)
				_batteryLabel.Text = "";
			else
				_batteryLabel.Text = _batteryCount.ToString();
		}
	}
	
	// ==================== РАБОТА С КЛЮЧАМИ ====================
	
	public void CollectKey(string keyName)
	{
		_keyCount++;
		
		_keyIcon.Visible = true;
		
		_keyIcon.Scale = new Vector2(0.5f, 0.5f);
		var tween = CreateTween();
		tween.TweenProperty(_keyIcon, "scale", new Vector2(1f, 1f), 0.3f)
			 .SetTrans(Tween.TransitionType.Elastic);
		
		// Показываем картинку "+ Ключ"
		ShowPlusImage("key", 1);
		
		if (_portal != null)
			_portal.ActivatePortal();
		
		GD.Print($"Собран {keyName}! Всего ключей: {_keyCount}");
	}
	
	public bool HasKey(string keyName)
	{
		return _keyCount > 0;
	}
	
	public bool UseKey(string keyName)
	{
		if (_keyCount > 0)
		{
			_keyCount--;
			
			if (_keyCount == 0)
			{
				_keyIcon.Visible = false;
			}
			
			return true;
		}
		return false;
	}
	
	// ==================== UI ОБНОВЛЕНИЕ ====================
	
	public void ShowActionMessage(string text, Color color)
	{
		if (_messageActive) return;
		_messageActive = true;
		
		_actionMessage.Text = text;
		_actionMessage.Modulate = color;
		
		var tween = CreateTween();
		tween.TweenProperty(_actionMessage, "modulate", new Color(color.R, color.G, color.B, 1), 0.2f);
		tween.TweenProperty(_actionMessage, "modulate", new Color(color.R, color.G, color.B, 0), 1.3f);
		tween.Finished += () => _messageActive = false;
	}
	
	// ==================== ОБРАБОТКА ВВОДА ====================
	
	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("use_battery"))
		{
			TryUseBattery();
		}
	}
	
	private void TryUseBattery()
	{
		if (UseBattery())
		{
			ShowPlusImage("use_battery");
		}
		else
		{
			ShowPlusImage("no_battery");
		}
	}
	
	// ==================== ДВИЖЕНИЕ И АНИМАЦИИ ====================
	
	public override void _Process(double delta)
	{
		Vector2 movement = MovementVector();
		Vector2 direction = movement.Normalized();
		
		float targetSpeed = WalkSpeed;
		bool isRunning = Input.GetActionStrength("run") > 0 && movement.Length() > 0;
		
		if (isRunning)
			targetSpeed = RunSpeed;
		
		_currentSpeed = Mathf.Lerp(_currentSpeed, targetSpeed, (float)delta * Acceleration);
		Velocity = _currentSpeed * direction;
		MoveAndSlide();
		
		UpdateAnimation(movement.Length() > 0);
		UpdateFlashlight(direction, movement.Length() > 0);
		UpdateFlashlightBattery((float)delta);
	}
	
	private void UpdateFlashlightBattery(float delta)
	{
		if (_flashlight == null) return;
		
		if (_flashlight.Enabled)
		{
			_currentBattery -= BatteryDrainRate * delta;
			
			if (_currentBattery <= 0)
			{
				_currentBattery = 0;
				_flashlight.Enabled = false;
				ShowActionMessage("🔋 Фонарик разрядился!", new Color(1, 0.5f, 0));
			}
			
			float energy = Mathf.Clamp(_currentBattery / MaxBattery * 0.8f + 0.2f, 0.2f, 1f);
			_flashlight.Energy = energy;
		}
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
		
		Vector2 targetDirection = isMoving && direction != Vector2.Zero ? direction : _lastDirection;
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
	
	// ==================== СИСТЕМА ЗДОРОВЬЯ ====================
	
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
		
		_healthRegenDelayTimer.Stop();
		_healthRegenTimer.Stop();
		
		_currentHealth -= damage;
		
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
		if (_heartbeatTween != null && _heartbeatTween.IsRunning())
		{
			_heartbeatTween.Kill();
		}
		
		float intensity;
		
		if (_currentHealth == 3)
			intensity = 0.15f;
		else if (_currentHealth == 2)
			intensity = 0.3f;
		else if (_currentHealth == 1)
			intensity = 0.5f;
		else
			intensity = 0.7f;
		
		_damageOverlay.Color = new Color(1, 0, 0, intensity);
		_isInjured = true;
	}
	
	private void StartHeartbeat()
	{
		if (_heartbeatTween != null && _heartbeatTween.IsRunning())
		{
			_heartbeatTween.Kill();
		}
		
		float baseIntensity = _damageOverlay.Color.A;
		
		if (baseIntensity <= 0.01f) return;
		
		_heartbeatTween = CreateTween();
		_heartbeatTween.SetLoops();
		
		_heartbeatTween.TweenProperty(_damageOverlay, "color", new Color(1, 0, 0, baseIntensity + 0.05f), 0.15f);
		_heartbeatTween.TweenProperty(_damageOverlay, "color", new Color(1, 0, 0, baseIntensity - 0.03f), 0.3f);
		_heartbeatTween.TweenProperty(_damageOverlay, "color", new Color(1, 0, 0, baseIntensity + 0.05f), 0.15f);
		_heartbeatTween.TweenProperty(_damageOverlay, "color", new Color(1, 0, 0, baseIntensity - 0.03f), 0.3f);
	}
	
	private void StopHeartbeat()
	{
		if (_heartbeatTween != null && _heartbeatTween.IsRunning())
		{
			_heartbeatTween.Kill();
		}
		
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
		
		if (_heartbeatTween != null && _heartbeatTween.IsRunning())
		{
			_heartbeatTween.Kill();
		}
		
		_healthRegenTimer.Start();
	}
	
	private void RegenerateHealth()
	{
		if (_isDead) return;
		
		if (_currentHealth >= MaxHealth)
		{
			_healthRegenTimer.Stop();
			StopHeartbeat();
			GD.Print("Здоровье полностью восстановлено!");
			return;
		}
		
		_currentHealth++;
		GD.Print($"Регенерация: {_currentHealth}/{MaxHealth}");
		
		if (_heartbeatTween != null && _heartbeatTween.IsRunning())
		{
			_heartbeatTween.Kill();
		}
		
		if (_currentHealth < MaxHealth)
		{
			float newIntensity;
			
			if (_currentHealth == 2)
				newIntensity = 0.3f;
			else if (_currentHealth == 1)
				newIntensity = 0.5f;
			else
				newIntensity = 0.15f;
			
			_damageOverlay.Color = new Color(1, 0, 0, newIntensity);
			StartHeartbeat();
		}
		else
		{
			StopHeartbeat();
		}
	}
	
	private async void Die()
	{
		_isDead = true;
		GD.Print("ИГРОК УМЕР!");
		
		StopHeartbeat();
		
		SetProcess(false);
		SetPhysicsProcess(false);
		
		var deathTween = CreateTween();
		deathTween.TweenProperty(_damageOverlay, "color", new Color(1, 0, 0, 0.8f), 0.3f);
		await ToSignal(deathTween, Tween.SignalName.Finished);
		
		Respawn();
		
		var respawnTween = CreateTween();
		respawnTween.TweenProperty(_damageOverlay, "color", new Color(1, 0, 0, 0), 0.5f);
		await ToSignal(respawnTween, Tween.SignalName.Finished);
		
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
			Position = new Vector2(50, 60);
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
