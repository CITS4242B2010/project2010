using System;
using System.Collections.Generic;
using Engine;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.UISystem;
using Engine.FileSystem;
using Engine.PhysicsSystem;
using Engine.Renderer;
using Engine.SoundSystem;
using Engine.MathEx;
using Engine.Utils;
using GameCommon;
using GameEntities;

namespace Maps_PhysicsDemo_LogicSystem_LogicSystemScripts
{
	public class Terminal_0 : Engine.EntitySystem.LogicSystem.LogicEntityObject
	{
		GameEntities.GameGuiObject __ownerEntity;
		
		public Terminal_0( GameEntities.GameGuiObject ownerEntity )
			: base( ownerEntity )
		{
			this.__ownerEntity = ownerEntity;
			ownerEntity.PostCreated += delegate( Engine.EntitySystem.Entity __entity, System.Boolean loaded ) { if( Engine.EntitySystem.LogicSystemManager.Instance != null )PostCreated( loaded ); };
			ownerEntity.Destroying += delegate( Engine.EntitySystem.Entity __entity ) { if( Engine.EntitySystem.LogicSystemManager.Instance != null )Destroying(  ); };
			ownerEntity.Render += delegate( Engine.MapSystem.MapObject __entity, Engine.Renderer.Camera camera ) { if( Engine.EntitySystem.LogicSystemManager.Instance != null )Render( camera ); };
			ownerEntity.Tick += delegate( Engine.EntitySystem.Entity __entity ) { if( Engine.EntitySystem.LogicSystemManager.Instance != null )Tick(  ); };
		}
		
		public GameEntities.GameGuiObject Owner
		{
			get { return __ownerEntity; }
		}
		
		[Engine.EntitySystem.Entity.FieldSerialize]
		public Engine.MapSystem.MapObject hangingBall;
		[Engine.EntitySystem.Entity.FieldSerialize]
		public System.Single ccdTestRemainingTime;
		[Engine.EntitySystem.Entity.FieldSerialize]
		public System.Int32 ccdTestRemainingCount;
		
		public void PostCreated( System.Boolean loaded )
		{
			( (EButton)Owner.MainControl.Controls[ "Boxes" ] ).Click += delegate(EButton sender)
			{
				CreateManyObjects( (MapObjectType)EntityTypes.Instance.GetByName( "Box" ), true );
			};
			
			( (EButton)Owner.MainControl.Controls[ "Barrels" ] ).Click += delegate(EButton sender)
			{
				CreateManyObjects( (MapObjectType)EntityTypes.Instance.GetByName( "ExplosionBarrel" ), true );
			};
			
			( (EButton)Owner.MainControl.Controls[ "Carts" ] ).Click += delegate(EButton sender)
			{
				CreateManyObjects( (MapObjectType)EntityTypes.Instance.GetByName( "Cart" ), false );
			};
			
			( (EButton)Owner.MainControl.Controls[ "CCDTest" ] ).Click += delegate(EButton sender)
			{
				ccdTestRemainingCount += 10;
			};
			
			( (EButton)Owner.MainControl.Controls[ "WoodBoxes" ] ).Click += delegate(EButton sender)
			{
				CreateManyObjects( (MapObjectType)EntityTypes.Instance.GetByName( "WoodBox" ), false );
			};
			
			((EScrollBar)Owner.MainControl.Controls[ "CartsThrottle" ]).ValueChange += delegate(EScrollBar sender)
			{
				UpdateCartsThrottle();
			};
			
			((EScrollBar)Owner.MainControl.Controls[ "Fans" ]).ValueChange += fansScrollBar_ValueChange;
			((ECheckBox)Owner.MainControl.Controls[ "HangingBall" ]).Checked = hangingBall != null;
			((ECheckBox)Owner.MainControl.Controls[ "HangingBall" ]).CheckedChange += hangingBallCheckBox_CheckedChange;
			
			//jumpPadsCheckBox = (ECheckBox)MainControl.Controls[ "JumpPads" ];
			//jumpPadsCheckBox.CheckedChange += jumpPadsCheckBox_CheckedChange;
			
			//( (EButton)MainControl.Controls[ "Run" ] ).Click += run_Click;
			( (EButton)Owner.MainControl.Controls[ "Clear" ] ).Click += clear_Click;
			
			((EScrollBar)Owner.MainControl.Controls[ "Gravity" ]).ValueChange += delegate(EScrollBar sender)
			{
				PhysicsWorld.Instance.Gravity = new Vec3(0,0,-sender.Value);
			};
			
			Owner.AddTimer();
			
			UpdateCartsThrottle();
			
		}

		public void clear_Click( Engine.UISystem.EButton sender )
		{
			foreach( Entity entity in Map.Instance.Children )
			{
				bool delete = false;
			
				string str = entity.UserData as string;
				if( str != null && str == "AllowClear" )
					delete = true;
			
				if( entity is Corpse )
					delete = true;
			
				string prefix = "WoodBox";
				if( entity.Type.Name.Length > prefix.Length && entity.Type.Name.Substring(0, 7) == prefix )
					delete = true;
			
				if(entity.Type.Name == "Ball")
					delete = true;
			
				if(delete)
					entity.SetDeleted();
			}
			
		}

		public void CreateManyObjects( Engine.MapSystem.MapObjectType mapObjectType, System.Boolean many )
		{
			const float zombieProbability = .01f;
			
			int step = many ? 5 : 20;
			
			for( float y = -10; y < 11; y += step )
			{
				for( float x = -10; x < 11; x += step )
				{
					if( x == 0 && y == 0 )
						continue;
			
					Vec3 pos = new Vec3( x, y, 10 );
					if(mapObjectType.Name == "WoodBox")
						pos.Z = 2;
			
					//Check busy
					bool busy = false;
					Map.Instance.GetObjects(new Bounds(pos - new Vec3(.75f, .75f, .75f), pos + new Vec3(.75f, .75f, .75f)), 
						delegate(MapObject o)
						{
							if(!(o is Region || o is StaticMesh))
								busy = true;
						});
					if(busy)
						continue;
			
					EntityType type;
			
					if( World.Instance.Random.NextFloat() < zombieProbability )
						type = EntityTypes.Instance.GetByName( "Zombie" );
					else
						type = mapObjectType;
			
					MapObject obj = (MapObject)Entities.Instance.Create( type, Map.Instance );
					obj.UserData = "AllowClear";
					obj.Position = pos;
			
					if( type == mapObjectType )
					{
						float dir = World.Instance.Random.NextFloat() * MathFunctions.PI;
						float halfAngle = dir * 0.5f;
			
						if(mapObjectType.Name == "WoodBox")
							obj.Rotation = new Quat( new Vec3( 0, 0, MathFunctions.Sin( halfAngle ) ), MathFunctions.Cos( halfAngle ) );
						else
							obj.Rotation = new Quat( new Vec3( 0, MathFunctions.Sin( halfAngle ), 0 ), MathFunctions.Cos( halfAngle ) );
					}
			
					obj.PostCreate();
				}
			}
			
			if(mapObjectType.Name == "Cart")
				UpdateCartsThrottle();
			
		}

		public void fansScrollBar_ValueChange( Engine.UISystem.EScrollBar sender )
		{
			foreach( Entity entity in Map.Instance.Children )
			{
				Fan fan = entity as Fan;
				if( fan != null )
					fan.Throttle = sender.Value;
			}
		}

		public void hangingBallCheckBox_CheckedChange( Engine.UISystem.ECheckBox sender )
		{
			if( sender.Checked )
			{
				EntityType type = EntityTypes.Instance.GetByName( "HangingBall" );
				hangingBall = (MapObject)Entities.Instance.Create( type, Map.Instance );
				hangingBall.Position = new Vec3( 0, 0, 13 );
				hangingBall.PostCreate();
			}
			else
			{
				if( hangingBall != null )
				{
					hangingBall.SetDeleted();
					hangingBall = null;
				}
			}
			
			/*!!!!!!!
			void jumpPadsCheckBox_CheckedChange( object sender )
			{
				if(jumpPadsCheckBox.Checked)
				{
					EntityType type = EntityTypes.Instance.Find("JumpPad");
			
					const float distance = 10;
					const float posZ = .5f;
			
					Vec3[] positions = new Vec3[]{
						new Vec3(-distance, -distance, posZ),
						new Vec3(distance, -distance, posZ),
						new Vec3(-distance, distance, posZ),
						new Vec3(distance, distance, posZ)};
			
			
					for(int n = 0; n < 4; n++)
					{
						JumpPad jumpPad = (JumpPad)Entities.Instance.Create(type, Map.Instance);
						jumpPad.Position = positions[n];
						//jumpPad.Rotation = rotations[n];
						jumpPad.PostCreate();
					}
				}
				else
				{
					foreach( Entity entity in Map.Instance.Children )
						if(entity as JumpPad != null)
							entity.SetDeleted();
				}
			}*/
			
		}

		public void Destroying()
		{
			PhysicsWorld.Instance.Gravity = new Vec3(0,0,-9.81f);
		}

		public void UpdateCartsThrottle()
		{
			float throttle = ((EScrollBar)Owner.MainControl.Controls[ "CartsThrottle" ]).Value;
			
			
			foreach( Entity entity in Map.Instance.Children )
			{
				MapObject obj = entity as MapObject;
				if(obj == null)
					continue;
			
				if(obj.Type.Name != "Cart")
					continue;
			
				foreach(Motor motor in obj.PhysicsModel.Motors)
				{
					GearedMotor gearedMotor = motor as GearedMotor;
					if(gearedMotor == null)
						continue;
			
					gearedMotor.Enabled = true;
					gearedMotor.Throttle = throttle;
				}
			}
		}

		public void Render( Engine.Renderer.Camera camera )
		{
			if(camera != RendererWorld.Instance.DefaultCamera)
				return;
			
			bool rayCastTest = ((ECheckBox)Owner.MainControl.Controls[ "RayCastTest" ]).Checked;
			bool piercingRayCastTest = ((ECheckBox)Owner.MainControl.Controls[ "PiercingRayCastTest" ]).Checked;
			bool volumeCastTest = ((ECheckBox)Owner.MainControl.Controls[ "VolumeCastTest" ]).Checked;
			
			if(rayCastTest)
			{
				for(float y = -18; y < 18; y++)
				{
					Ray ray = new Ray(new Vec3(0,y, 20), new Vec3(0,0,-100));
			
					RayCastResult result = PhysicsWorld.Instance.RayCast( ray, (int)ContactGroup.CastOnlyContact );
			
					if(result.Shape != null)
					{
						camera.DebugGeometry.Color = new ColorValue(1,1,0);
						camera.DebugGeometry.AddLine(ray.Origin, result.Position);
			
						camera.DebugGeometry.Color = new ColorValue(1,0,0);
						camera.DebugGeometry.AddSphere(new Sphere(result.Position, .1f), 4);
			
						camera.DebugGeometry.Color = new ColorValue(0,1,0);
						camera.DebugGeometry.AddArrow(result.Position, result.Position + result.Normal * .3f);
					}
				}
			}
			
			if(piercingRayCastTest)
			{
				for(float y = -18; y < 18; y++)
				{
					Ray ray = new Ray(new Vec3(18,y, .5f), new Vec3(-36,0,0));
			
					RayCastResult[] piercingResult = PhysicsWorld.Instance.RayCastPiercing( 
						ray, (int)ContactGroup.CastOnlyContact );
			
					camera.DebugGeometry.Color = new ColorValue(0,0,1);
					camera.DebugGeometry.AddLine(ray.Origin, ray.Origin + ray.Direction);
			
					foreach(RayCastResult result in piercingResult)
					{
						camera.DebugGeometry.Color = new ColorValue(1,0,0);
						camera.DebugGeometry.AddSphere(new Sphere(result.Position, .1f), 4);
			
						camera.DebugGeometry.Color = new ColorValue(0,1,0);
						camera.DebugGeometry.AddArrow(result.Position, result.Position + result.Normal * .3f);
					}
				}
			}
			
			if(volumeCastTest)
			{
				Bounds bounds = new Bounds(new Vec3(-10, -10, .2f), new Vec3(10, 10, 10.2f));
			
				camera.DebugGeometry.Color = new ColorValue(1,1,0);
				camera.DebugGeometry.AddBounds(bounds);
			
				Body[] result = PhysicsWorld.Instance.VolumeCast(bounds, (int)ContactGroup.CastOnlyDynamic);
				foreach(Body body in result)
				{
					camera.DebugGeometry.Color = new ColorValue(0,1,0);
					camera.DebugGeometry.AddSphere(new Sphere(body.Position, 1), 32);
				}
			
			/*
				Box box = new Box();
				box.Center = new Vec3(0,0,5);
				box.Extents = new Vec3(3,3,7);
				box.Axis = new Angles(50, 30, 20).ToQuat().ToMat3();
			
				DebugGeometry.Instance.Color = new ColorValue(1,1,0);
				DebugGeometry.Instance.AddBox(box);
			
				Body[] result = PhysicsWorld.Instance.VolumeCast(box, (int)ContactGroup.CastOnlyDynamic);
				foreach(Body body in result)
				{
					DebugGeometry.Instance.Color = new ColorValue(0,1,0);
					DebugGeometry.Instance.AddSphere(new Sphere(body.Position, 1), 32);
				}
			*/
			
			/*
				Sphere sphere = new Sphere(new Vec3(0,0,5), 7);
			
				DebugGeometry.Instance.Color = new ColorValue(1,1,0);
				DebugGeometry.Instance.AddSphere(sphere);
			
				Body[] result = PhysicsWorld.Instance.VolumeCast(sphere, (int)ContactGroup.CastOnlyDynamic);
				foreach(Body body in result)
				{
					DebugGeometry.Instance.Color = new ColorValue(0,1,0);
					DebugGeometry.Instance.AddSphere(new Sphere(body.Position, 1), 32);
				}
			*/
			
			/*
				Capsule capsule = new Capsule();
				capsule.Point1 = new Vec3(-10,0,5);
				capsule.Point2 = new Vec3(5,10,2);
				capsule.Radius = 4;
			
				DebugGeometry.Instance.Color = new ColorValue(1,1,0);
				DebugGeometry.Instance.AddSphere(new Sphere(capsule.Point1, .1f));
				DebugGeometry.Instance.AddSphere(new Sphere(capsule.Point2, .1f));
				DebugGeometry.Instance.AddCapsule(capsule);
			
				Body[] result = PhysicsWorld.Instance.VolumeCast(capsule, (int)ContactGroup.CastOnlyDynamic);
				foreach(Body body in result)
				{
					DebugGeometry.Instance.Color = new ColorValue(0,1,0);
					DebugGeometry.Instance.AddSphere(new Sphere(body.Position, 1), 32);
				}
			*/
			
			}
			
		}

		public void Tick()
		{
			
			//CCD Test
			if( ccdTestRemainingCount > 0 )
			{
				ccdTestRemainingTime -= Entity.TickDelta;
				if(ccdTestRemainingTime <= 0)
				{
					ccdTestRemainingTime = .1f;
					ccdTestRemainingCount--;
				
					//create entity
				
					MapObject obj = (MapObject)Entities.Instance.Create("SmallBox", Map.Instance);
					obj.Position = new Vec3(0,0,.3f);
					obj.UserData = "AllowClear";
					obj.PostCreate();
			
					if(obj.PhysicsModel != null)
					{
						Radian angle = World.Instance.Random.NextFloat() * MathFunctions.PI * 2;
			
						Vec3 linearVelocity = new Vec3(
							MathFunctions.Cos(angle) * 100, MathFunctions.Sin(angle) * 100, 60);
			
						foreach(Body body in obj.PhysicsModel.Bodies)
							body.LinearVelocity = linearVelocity;
					}
			
					//sound
					Dynamic dynamic = obj as Dynamic;
					if(dynamic != null)
						dynamic.SoundPlay3D( "Types\\Weapons\\SubmachineGun\\AlternativeFire.ogg", 1, false );
				}
			}
			
		}

	}
}
