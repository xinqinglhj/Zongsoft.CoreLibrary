﻿using System;
using System.Linq;
using System.Diagnostics;

namespace Zongsoft.Samples.Entities
{
	public class Program
	{
		private const int COUNT = 10000 * 100;

		static void Main(string[] args)
		{
			//TestChanges();

			Performance(COUNT);
			PerformanceDynamic(COUNT);

			BuildTest();

			Console.ForegroundColor = ConsoleColor.DarkMagenta;
			Console.WriteLine();
			Console.WriteLine("Press enter key to exit.");
			Console.ResetColor();

			Console.ReadLine();
		}

		private static void BuildTest()
		{
			//Data.Entity.Build<Models.IManager>();
			//Data.Entity.Save();

			//return;

			var person = Data.Entity.Build<Models.IPerson>();
			var person1 = Data.Entity.Build<Models.IPerson>();
			var user = Data.Entity.Build<Models.IUserEntity>();
			var myentity = Data.Entity.Build<Models.IMyEntity>();
			var employee = Data.Entity.Build<Models.IEmployee>();
			var manager = Data.Entity.Build<Models.IManager>();

			Data.Entity.Save();

			//验证实体属性变更计数方法
			TestCount();

			var property = user.GetType().GetProperty("Namespace");

			if(employee is System.ComponentModel.INotifyPropertyChanged notify)
				notify.PropertyChanged += Entity_PropertyChanged;

			employee.UserId = 100;
			employee.Name = "Popeye";
			employee.Name = "Popeye";
			employee.Name = "Popeye";
			employee.FullName = "Popeye Zhong";
			employee.Status = 1;
			employee.StatusTimestamp = DateTime.Now;
			employee.Avatar = "nothing";
			employee.TrySetValue("Avatar", "/data/Zongsoft/Zongsoft.CoreLibrary/README.md");
			employee.TrySetValue("Email", "zongsoft@qq.com");
			employee.TrySetValue("Description", "Here is the description.");

			Console.WriteLine();

			if(employee.TryGetValue("Name", out var name))
				Console.WriteLine($"Name: {name}");
			if(employee.TryGetValue("FullName", out var fullName))
				Console.WriteLine($"FullName: {fullName}");
			if(employee.TryGetValue("Email", out var email))
				Console.WriteLine($"Email: {email}");
			if(employee.TryGetValue("AvatarUrl", out var avatarUrl))
				Console.WriteLine($"AvatarUrl: {avatarUrl}");
			if(employee.TryGetValue("NoExists", out var noExists))
				Console.WriteLine($"NoExists: {noExists}");

			Console.WriteLine();
			Console.WriteLine("HasChanges(Email, Name): " + employee.HasChanges("Email", "Name"));
			Console.WriteLine();

			DisplayChanges(employee);
		}

		private static void Performance(int count)
		{
			Console.WriteLine($"To run performance {count.ToString("#,##0")} times.");
			Console.WriteLine("**********************************");

			Func<Models.UserEntity> creator = new Func<Models.UserEntity>(() => new Models.UserEntity());

			var stopwatch = new Stopwatch();

			stopwatch.Start();

			for(var i = 0; i < count; i++)
			{
				var user = new Models.User()
				{
					UserId = (uint)i,
					Avatar = ":smile:",
					Name = "Name: " + i.ToString(),
					FullName = "FullName",
					Namespace = "Zongsoft",
					Status = (byte)(i % byte.MaxValue),
					StatusTimestamp = (i % 11 == 0) ? DateTime.Now : DateTime.MinValue,
					CreatedTime = DateTime.Now,
				};
			}

			Console.WriteLine($"Native Object: {stopwatch.ElapsedMilliseconds}");

			stopwatch.Restart();

			for(var i = 0; i < count; i++)
			{
				var user = new Models.UserEntity()
				{
					UserId = (uint)i,
					Avatar = ":smile:",
					Name = "Name: " + i.ToString(),
					FullName = "FullName",
					Namespace = "Zongsoft",
					Status = (byte)(i % byte.MaxValue),
					StatusTimestamp = (i % 11 == 0) ? DateTime.Now : DateTime.MinValue,
					CreatedTime = DateTime.Now,
				};
			}

			Console.WriteLine($"Data Entity: {stopwatch.ElapsedMilliseconds}");

			stopwatch.Restart();

			for(var i = 0; i < count; i++)
			{
				var user = creator();

				user.UserId = (uint)i;
				user.Avatar = ":smile:";
				user.Name = "Name: " + i.ToString();
				user.FullName = "FullName";
				user.Namespace = "Zongsoft";
				user.Status = (byte)(i % byte.MaxValue);
				user.StatusTimestamp = (i % 11 == 0) ? DateTime.Now : DateTime.MinValue;
				user.CreatedTime = DateTime.Now;
			}

			Console.WriteLine($"Data Entity(Creator): {stopwatch.ElapsedMilliseconds}");

			stopwatch.Restart();

			for(var i = 0; i < count; i++)
			{
				Models.IUserEntity user = new Models.UserEntity();

				user.TrySetValue("UserId", (uint)i);
				user.TrySetValue("Avatar", ":smile:");
				user.TrySetValue("Name", "Name: " + i.ToString());
				user.TrySetValue("FullName", "FullName");
				user.TrySetValue("Namespace", "Zongsoft");
				user.TrySetValue("Status", (byte)(i % byte.MaxValue));
				user.TrySetValue("StatusTimestamp", (i % 11 == 0) ? DateTime.Now : DateTime.MinValue);
				user.TrySetValue("CreatedTime", DateTime.Now);
			}

			Console.WriteLine($"Data Entity(TrySet): {stopwatch.ElapsedMilliseconds}");
		}

		private static void PerformanceDynamic(int count)
		{
			var creator = Data.Entity.GetCreator(typeof(Models.IUserEntity)); //预先编译
			Data.Entity.Build<Models.IUserEntity>(); //预热（预先编译）

			var stopwatch = new Stopwatch();

			stopwatch.Start();

			for(int i = 0; i < count; i++)
			{
				var user = (Models.IUserEntity)creator();

				user.UserId = (uint)i;
				user.Avatar = ":smile:";
				user.Name = "Name: " + i.ToString();
				user.FullName = "FullName";
				user.Namespace = "Zongsoft";
				user.Status = (byte)(i % byte.MaxValue);
				user.StatusTimestamp = (i % 11 == 0) ? DateTime.Now : DateTime.MinValue;
				user.CreatedTime = DateTime.Now;
			}

			//var entities = Data.Entity.Build<Models.IUserEntity>(count, (entity, index) =>
			//{
			//	entity.UserId = (uint)index;
			//	entity.Avatar = ":smile:";
			//	entity.Name = "Name: " + index.ToString();
			//	entity.FullName = "FullName";
			//	entity.Namespace = "Zongsoft";
			//	entity.Status = (byte)(index % byte.MaxValue);
			//	entity.StatusTimestamp = (index % 11 == 0) ? DateTime.Now : DateTime.MinValue;
			//	entity.CreatedTime = DateTime.Now;
			//});

			//foreach(var entity in entities)
			//{
			//}

			//int index = 0;
			//var entities = Data.Entity.Build<Models.IUserEntity>(count);

			//foreach(var user in entities)
			//{
			//	user.UserId = (uint)index;
			//	user.Avatar = ":smile:";
			//	user.Name = "Name: " + index.ToString();
			//	user.FullName = "FullName";
			//	user.Namespace = "Zongsoft";
			//	user.Status = (byte)(index % byte.MaxValue);
			//	user.StatusTimestamp = (index % 11 == 0) ? DateTime.Now : DateTime.MinValue;
			//	user.CreatedTime = DateTime.Now;
			//}

			//var iterator = entities.GetEnumerator();
			//while(iterator.MoveNext())
			//{
			//	var user = iterator.Current;

			//	user.UserId = (uint)index;
			//	user.Avatar = ":smile:";
			//	user.Name = "Name: " + index.ToString();
			//	user.FullName = "FullName";
			//	user.Namespace = "Zongsoft";
			//	user.Status = (byte)(index % byte.MaxValue);
			//	user.StatusTimestamp = (index % 11 == 0) ? DateTime.Now : DateTime.MinValue;
			//	user.CreatedTime = DateTime.Now;

			//	index++;
			//}

			Console.WriteLine($"Dynamic Entity: {stopwatch.ElapsedMilliseconds}");

			stopwatch.Stop();
		}

		private static void TestChanges()
		{
			Models.IUserEntity entity = new Models.UserEntity();

			Console.WriteLine($"HasChanges: {entity.HasChanges()}");

			entity.UserId = 100;
			entity.UserId = 200;
			entity.Name = "Popeye";
			entity.Namespace = "Zongsoft";
			entity.Avatar = ":smile:";
			entity.CreatedTime = DateTime.Now;

			Console.WriteLine("HasChanges(Name, Email): {0}", entity.HasChanges("Email", "Name"));

			entity.TrySetValue("Email", "zongsoft@qq.com");
			entity.TrySetValue("PhoneNumber", "+86.13812345678");

			Console.WriteLine($"HasChanges: {entity.HasChanges()}");
			DisplayChanges(entity);
		}

		private static void DisplayChanges(Zongsoft.Data.IEntity entity)
		{
			int index = 0;

			foreach(var entry in entity.GetChanges())
			{
				Console.WriteLine($"\t[{++index}] {entry.Key} = {entry.Value}");
			}
		}

		private static void TestCount()
		{
			var result = Data.Entity.Build<Models.IMyEntity>(entity =>
			{
				entity.P1 = 100;
				entity.P2 = "xxx";
				entity.P3 = "xxx";
				entity.P4 = "xxx";
				entity.P5 = "xxx";
				entity.P6 = "xxx";
				entity.P7 = "xxx";
				entity.P8 = "xxx";
				entity.P9 = "xxx";
				entity.P10 = "xxx";
				entity.P11 = "xxx";
				entity.P12 = "xxx";
				entity.P13 = "xxx";
				entity.P14 = "xxx";
				entity.P15 = "xxx";
				entity.P16 = "xxx";
				entity.P17 = "xxx";
				entity.P18 = "xxx";
				entity.P19 = "xxx";
				entity.P20 = "xxx";
				entity.P21 = "xxx";
				entity.P22 = "xxx";
				entity.P23 = "xxx";
				entity.P24 = "xxx";
				entity.P25 = "xxx";
				entity.P26 = "xxx";
				entity.P27 = "xxx";
				entity.P28 = "xxx";
				entity.P29 = "xxx";
				entity.P30 = "xxx";
				entity.P31 = "xxx";
				entity.P32 = "xxx";
			});

			if(result.Count() != 32)
			{
				Console.WriteLine();
				Console.WriteLine($"***** The Count method of the '{result.GetType().Name}' entity is validation failed. *****");
				Console.WriteLine();
			}

			result.Reset("P10", "No exists");
			if(result.Count() != 31)
			{
				Console.WriteLine();
				Console.WriteLine($"***** The Reset method of the '{result.GetType().Name}' entity is validation failed. *****");
				Console.WriteLine();
			}

			if(!result.Reset("P1", out var value))
			{
				Console.WriteLine();
				Console.WriteLine($"***** The Reset method of the '{result.GetType().Name}' entity is validation failed. *****");
				Console.WriteLine();
			}

			if(value == null || (int)value != 100)
			{
				Console.WriteLine();
				Console.WriteLine($"***** The Reset method of the '{result.GetType().Name}' entity is validation failed. *****");
				Console.WriteLine();
			}
		}

		private static void Entity_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			Console.WriteLine("PropertyChanged: " + e.PropertyName);
		}
	}
}
