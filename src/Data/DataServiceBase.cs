﻿/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@qq.com>
 *
 * Copyright (C) 2016-2019 Zongsoft Corporation <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.CoreLibrary.
 *
 * Zongsoft.CoreLibrary is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 *
 * Zongsoft.CoreLibrary is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
 * Lesser General Public License for more details.
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with Zongsoft.CoreLibrary; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA
 */

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Data
{
	public class DataServiceBase<TEntity> : IDataService<TEntity>
	{
		#region 事件定义
		public event EventHandler<DataGettedEventArgs<TEntity>> Getted;
		public event EventHandler<DataGettingEventArgs<TEntity>> Getting;

		public event EventHandler<DataCountedEventArgs> Counted;
		public event EventHandler<DataCountingEventArgs> Counting;
		public event EventHandler<DataExecutedEventArgs> Executed;
		public event EventHandler<DataExecutingEventArgs> Executing;
		public event EventHandler<DataExistedEventArgs> Existed;
		public event EventHandler<DataExistingEventArgs> Existing;
		public event EventHandler<DataIncrementedEventArgs> Incremented;
		public event EventHandler<DataIncrementingEventArgs> Incrementing;
		public event EventHandler<DataDeletedEventArgs> Deleted;
		public event EventHandler<DataDeletingEventArgs> Deleting;
		public event EventHandler<DataInsertedEventArgs> Inserted;
		public event EventHandler<DataInsertingEventArgs> Inserting;
		public event EventHandler<DataUpdatedEventArgs> Updated;
		public event EventHandler<DataUpdatingEventArgs> Updating;
		public event EventHandler<DataSelectedEventArgs> Selected;
		public event EventHandler<DataSelectingEventArgs> Selecting;
		#endregion

		#region 成员字段
		private string _name;
		private IDataAccess _dataAccess;
		private IDataSearcher<TEntity> _searcher;
		private Services.IServiceProvider _serviceProvider;
		#endregion

		#region 构造函数
		protected DataServiceBase(Services.IServiceProvider serviceProvider)
		{
			_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
			_dataAccess = serviceProvider.ResolveRequired<IDataAccess>();

			//创建数据搜索器
			_searcher = new InnerDataSearcher(this, (DataSearcherAttribute[])Attribute.GetCustomAttributes(this.GetType(), typeof(DataSearcherAttribute), true));
		}

		protected DataServiceBase(string name, Services.IServiceProvider serviceProvider)
		{
			if(string.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException(nameof(name));

			_name = name.Trim();
			_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
			_dataAccess = serviceProvider.ResolveRequired<IDataAccess>();

			//创建数据搜索器
			_searcher = new InnerDataSearcher(this, (DataSearcherAttribute[])Attribute.GetCustomAttributes(this.GetType(), typeof(DataSearcherAttribute), true));
		}
		#endregion

		#region 公共属性
		public string Name
		{
			get
			{
				if(string.IsNullOrEmpty(_name))
				{
					var dataAccess = this.DataAccess;

					if(dataAccess != null && dataAccess.Naming != null)
						_name = dataAccess.Naming.Get<TEntity>();
				}

				return _name;
			}
			protected set
			{
				if(string.IsNullOrWhiteSpace(value))
					throw new ArgumentNullException();

				_name = value.Trim();
			}
		}

		public virtual bool CanDelete
		{
			get => true;
		}

		public virtual bool CanInsert
		{
			get => true;
		}

		public virtual bool CanUpdate
		{
			get => true;
		}

		public virtual bool CanUpsert
		{
			get => true;
		}

		public IDataAccess DataAccess
		{
			get
			{
				if(_dataAccess == null)
					_dataAccess = _serviceProvider.Resolve<IDataAccess>();

				return _dataAccess;
			}
			set
			{
				_dataAccess = value ?? throw new ArgumentNullException();
			}
		}

		public IDataSearcher<TEntity> Searcher
		{
			get => _searcher;
			set => _searcher = value ?? throw new ArgumentNullException();
		}

		public Zongsoft.Services.IServiceProvider ServiceProvider
		{
			get
			{
				return _serviceProvider;
			}
			set
			{
				if(value == null)
					throw new ArgumentNullException();

				_serviceProvider = value;
			}
		}
		#endregion

		#region 保护属性
		protected virtual Security.CredentialPrincipal Principal
		{
			get
			{
				return Services.ApplicationContext.Current?.Principal as Zongsoft.Security.CredentialPrincipal;
			}
		}

		protected virtual Security.Credential Credential
		{
			get
			{
				var principal = this.Principal;

				if(principal != null && principal.Identity.IsAuthenticated)
					return principal.Identity.Credential;

				return null;
			}
		}
		#endregion

		#region 授权验证
		protected virtual void Authorize(Method method, object state)
		{
			var credential = this.Credential;

			if(credential == null || credential.User == null)
				throw new Security.Membership.AuthorizationException();
		}
		#endregion

		#region 执行方法
		public IEnumerable<T> Execute<T>(string name, IDictionary<string, object> inParameters, object state = null)
		{
			return this.Execute<T>(name, inParameters, out _, state);
		}

		public IEnumerable<T> Execute<T>(string name, IDictionary<string, object> inParameters, out IDictionary<string, object> outParameters, object state = null)
		{
			//进行授权验证
			this.Authorize(Method.Execute(), state);

			return this.OnExecute<T>(name, inParameters, out outParameters, state);
		}

		protected virtual IEnumerable<T> OnExecute<T>(string name, IDictionary<string, object> inParameters, out IDictionary<string, object> outParameters, object state)
		{
			return this.DataAccess.Execute<T>(name, inParameters, out outParameters, state, ctx => this.OnExecuting(ctx), ctx => this.OnExecuted(ctx));
		}

		public object ExecuteScalar(string name, IDictionary<string, object> inParameters, object state = null)
		{
			return this.ExecuteScalar(name, inParameters, out _, state);
		}

		public object ExecuteScalar(string name, IDictionary<string, object> inParameters, out IDictionary<string, object> outParameters, object state = null)
		{
			//进行授权验证
			this.Authorize(Method.Execute(), state);

			return this.OnExecuteScalar(name, inParameters, out outParameters, state);
		}

		protected virtual object OnExecuteScalar(string name, IDictionary<string, object> inParameters, out IDictionary<string, object> outParameters, object state)
		{
			return this.DataAccess.ExecuteScalar(name, inParameters, out outParameters, state, ctx => this.OnExecuting(ctx), ctx => this.OnExecuted(ctx));
		}
		#endregion

		#region 存在方法
		public bool Exists<TKey>(TKey key, object state = null)
		{
			return this.Exists(this.ConvertKey(key, out _), state);
		}

		public bool Exists<TKey1, TKey2>(TKey1 key1, TKey2 key2, object state = null)
		{
			return this.Exists(this.ConvertKey(key1, key2, out _), state);
		}

		public bool Exists<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, object state = null)
		{
			return this.Exists(this.ConvertKey(key1, key2, key3, out _), state);
		}

		public bool Exists(ICondition condition, object state = null)
		{
			//进行授权验证
			this.Authorize(Method.Exists(), state);

			//修整查询条件
			condition = this.OnValidate(Method.Exists(), condition);

			//执行存在操作
			return this.OnExists(condition, state);
		}

		protected virtual bool OnExists(ICondition condition, object state)
		{
			return this.DataAccess.Exists(this.Name, condition, state, ctx => this.OnExisting(ctx), ctx => this.OnExisted(ctx));
		}
		#endregion

		#region 计数方法
		public int Count(ICondition condition, object state)
		{
			return this.Count(condition, string.Empty, state);
		}

		public int Count(ICondition condition, string member)
		{
			return this.Count(condition, member, null);
		}

		public int Count(ICondition condition, string member = null, object state = null)
		{
			//进行授权验证
			this.Authorize(Method.Count(), state);

			//修整查询条件
			condition = this.OnValidate(Method.Count(), condition);

			//执行计数操作
			return this.OnCount(condition, member, state);
		}

		protected virtual int OnCount(ICondition condition, string member, object state)
		{
			return this.DataAccess.Count(this.Name, condition, member, state, ctx => this.OnCounting(ctx), ctx => this.OnCounted(ctx));
		}
		#endregion

		#region 递增方法
		public long Decrement(string member, ICondition condition, object state)
		{
			return this.Decrement(member, condition, 1, state);
		}

		public long Decrement(string member, ICondition condition, int interval)
		{
			return this.Decrement(member, condition, interval, null);
		}

		public long Decrement(string member, ICondition condition, int interval = 1, object state = null)
		{
			return this.Increment(member, condition, -interval, state);
		}

		public long Increment(string member, ICondition condition, object state)
		{
			return this.Increment(member, condition, 1, state);
		}

		public long Increment(string member, ICondition condition, int interval)
		{
			return this.Increment(member, condition, interval, null);
		}

		public long Increment(string member, ICondition condition, int interval = 1, object state = null)
		{
			//进行授权验证
			this.Authorize(Method.Increment(), state);

			//修整查询条件
			condition = this.OnValidate(Method.Increment(), condition);

			//执行递增操作
			return this.OnIncrement(member, condition, interval, state);
		}

		protected virtual long OnIncrement(string member, ICondition condition, int interval, object state)
		{
			return this.DataAccess.Increment(this.Name, member, condition, interval, state, ctx => this.OnIncrementing(ctx), ctx => this.OnIncremented(ctx));
		}
		#endregion

		#region 删除方法
		public int Delete<TKey>(TKey key, string schema = null)
		{
			return this.Delete<TKey>(key, schema, null);
		}

		public int Delete<TKey>(TKey key, string schema, object state)
		{
			//确认是否可以执行该操作
			this.EnsureDelete();

			//进行授权验证
			this.Authorize(Method.Delete(), state);

			//将删除键转换成条件对象，并进行修整
			var condition = this.OnValidate(Method.Delete(), this.ConvertKey(key, out _));

			//执行删除操作
			return this.OnDelete(condition, this.GetSchema(schema), state);
		}

		public int Delete<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema = null)
		{
			return this.Delete<TKey1, TKey2>(key1, key2, schema, null);
		}

		public int Delete<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, object state)
		{
			//确认是否可以执行该操作
			this.EnsureDelete();

			//进行授权验证
			this.Authorize(Method.Delete(), state);

			//将删除键转换成条件对象，并进行修整
			var condition = this.OnValidate(Method.Delete(), this.ConvertKey(key1, key2, out _));

			//执行删除操作
			return this.OnDelete(condition, this.GetSchema(schema), state);
		}

		public int Delete<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema = null)
		{
			return this.Delete<TKey1, TKey2, TKey3>(key1, key2, key3, schema, null);
		}

		public int Delete<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, object state)
		{
			//确认是否可以执行该操作
			this.EnsureDelete();

			//进行授权验证
			this.Authorize(Method.Delete(), state);

			//将删除键转换成条件对象，并进行修整
			var condition = this.OnValidate(Method.Delete(), this.ConvertKey(key1, key2, key3, out _));

			//执行删除操作
			return this.OnDelete(condition, this.GetSchema(schema), state);
		}

		public int Delete(ICondition condition, string schema = null)
		{
			return this.Delete(condition, schema, null);
		}

		public int Delete(ICondition condition, string schema, object state)
		{
			//确认是否可以执行该操作
			this.EnsureDelete();

			//进行授权验证
			this.Authorize(Method.Delete(), state);

			//修整删除条件
			condition = this.OnValidate(Method.Delete(), condition);

			//执行删除操作
			return this.OnDelete(condition, this.GetSchema(schema), state);
		}

		protected virtual int OnDelete(ICondition condition, ISchema schema, object state)
		{
			if(condition == null)
				throw new NotSupportedException("The condition cann't is null on delete operation.");

			return this.DataAccess.Delete(this.Name, condition, schema, state, ctx => this.OnDeleting(ctx), ctx => this.OnDeleted(ctx));
		}
		#endregion

		#region 插入方法
		public int Insert(object data)
		{
			return this.Insert(data, null, null);
		}

		public int Insert(object data, object state)
		{
			return this.Insert(data, null, state);
		}

		public int Insert(object data, string schema)
		{
			return this.Insert(data, schema, null);
		}

		public int Insert(object data, string schema, object state)
		{
			//确认是否可以执行该操作
			this.EnsureInsert();

			//进行授权验证
			this.Authorize(Method.Insert(), state);

			if(data == null)
				return 0;

			//将当前插入数据对象转换成数据字典
			var dictionary = DataDictionary.GetDictionary<TEntity>(data);

			//验证待新增的数据
			this.OnValidate(Method.Insert(), dictionary);

			return this.OnInsert(dictionary, this.GetSchema(schema, data.GetType()), state);
		}

		protected virtual int OnInsert(IDataDictionary<TEntity> data, ISchema schema, object state)
		{
			if(data == null || data.Data == null)
				return 0;

			//执行数据引擎的插入操作
			return this.DataAccess.Insert(this.Name, data, schema, state, ctx => this.OnInserting(ctx), ctx => this.OnInserted(ctx));
		}

		public int InsertMany(IEnumerable items)
		{
			return this.InsertMany(items, null, null);
		}

		public int InsertMany(IEnumerable items, object state)
		{
			return this.InsertMany(items, null, state);
		}

		public int InsertMany(IEnumerable items, string schema)
		{
			return this.InsertMany(items, schema, null);
		}

		public int InsertMany(IEnumerable items, string schema, object state)
		{
			//确认是否可以执行该操作
			this.EnsureInsert();

			//进行授权验证
			this.Authorize(Method.InsertMany(), state);

			if(items == null)
				return 0;

			//将当前插入数据集合对象转换成数据字典集合
			var dictionares = DataDictionary.GetDictionaries<TEntity>(items);

			foreach(var dictionary in dictionares)
			{
				//验证待新增的数据
				this.OnValidate(Method.InsertMany(), dictionary);
			}

			return this.OnInsertMany(dictionares, this.GetSchema(schema, Common.TypeExtension.GetElementType(items.GetType())), state);
		}

		protected virtual int OnInsertMany(IEnumerable<IDataDictionary<TEntity>> items, ISchema schema, object state)
		{
			if(items == null)
				return 0;

			//执行数据引擎的插入操作
			return this.DataAccess.InsertMany(this.Name, items, schema, state, ctx => this.OnInserting(ctx), ctx => this.OnInserted(ctx));
		}
		#endregion

		#region 更新方法
		public int Update<TKey>(object data, TKey key, object state = null)
		{
			return this.Update<TKey>(data, key, null, state);
		}

		public int Update<TKey>(object data, TKey key, string schema, object state = null)
		{
			return this.Update(data, this.ConvertKey(key, out _), schema, state);
		}

		public int Update<TKey1, TKey2>(object data, TKey1 key1, TKey2 key2, object state = null)
		{
			return this.Update<TKey1, TKey2>(data, key1, key2, null, state);
		}

		public int Update<TKey1, TKey2>(object data, TKey1 key1, TKey2 key2, string schema, object state = null)
		{
			return this.Update(data, this.ConvertKey(key1, key2, out _), schema, state);
		}

		public int Update<TKey1, TKey2, TKey3>(object data, TKey1 key1, TKey2 key2, TKey3 key3, object state = null)
		{
			return this.Update<TKey1, TKey2, TKey3>(data, key1, key2, key3, null, state);
		}

		public int Update<TKey1, TKey2, TKey3>(object data, TKey1 key1, TKey2 key2, TKey3 key3, string schema, object state = null)
		{
			return this.Update(data, this.ConvertKey(key1, key2, key3, out _), schema, state);
		}

		public int Update(object data, object state = null)
		{
			return this.Update(data, null, null, state);
		}

		public int Update(object data, string schema, object state = null)
		{
			return this.Update(data, null, schema, state);
		}

		public int Update(object data, ICondition condition, object state = null)
		{
			return this.Update(data, condition, null, state);
		}

		public int Update(object data, ICondition condition, string schema, object state = null)
		{
			//确认是否可以执行该操作
			this.EnsureUpdate();

			//进行授权验证
			this.Authorize(Method.Update(), state);

			//将当前更新数据对象转换成数据字典
			var dictionary = DataDictionary.GetDictionary<TEntity>(data);

			//如果指定了更新条件，则尝试将条件中的主键值同步设置到数据字典中
			if(condition != null)
			{
				//遍历当前数据服务的实体主键名
				foreach(var key in this.DataAccess.GetKey(this.Name))
				{
					condition.Match(key, c => dictionary.TrySetValue(c.Name, c.Value));
				}
			}

			//修整过滤条件
			condition = this.OnValidate(Method.Update(), condition ?? this.EnsureUpdateCondition(dictionary));

			//验证待更新的数据
			this.OnValidate(Method.Update(), dictionary);

			//执行更新操作
			return this.OnUpdate(dictionary, condition, this.GetSchema(schema, data.GetType()), state);
		}

		protected virtual int OnUpdate(IDataDictionary<TEntity> data, ICondition condition, ISchema schema, object state)
		{
			if(data == null || data.Data == null)
				return 0;

			return this.DataAccess.Update(this.Name, data, condition, schema, state, ctx => this.OnUpdating(ctx), ctx => this.OnUpdated(ctx));
		}

		public int UpdateMany(IEnumerable items, object state = null)
		{
			return this.UpdateMany(items, null, state);
		}

		public int UpdateMany(IEnumerable items, string schema, object state = null)
		{
			//确认是否可以执行该操作
			this.EnsureUpdate();

			//进行授权验证
			this.Authorize(Method.UpdateMany(), state);

			if(items == null)
				return 0;

			//将当前更新数据集合对象转换成数据字典集合
			var dictionares = DataDictionary.GetDictionaries<TEntity>(items);

			foreach(var dictionary in dictionares)
			{
				//验证待更新的数据
				this.OnValidate(Method.UpdateMany(), dictionary);
			}

			return this.OnUpdateMany(dictionares, this.GetSchema(schema, Common.TypeExtension.GetElementType(items.GetType())), state);
		}

		protected virtual int OnUpdateMany(IEnumerable<IDataDictionary<TEntity>> items, ISchema schema, object state)
		{
			if(items == null)
				return 0;

			return this.DataAccess.UpdateMany(this.Name, items, schema, state, ctx => this.OnUpdating(ctx), ctx => this.OnUpdated(ctx));
		}
		#endregion

		#region 查询方法

		#region 单键查询
		public object Get<TKey>(TKey key, params Sorting[] sortings)
		{
			return this.Get<TKey>(key, string.Empty, null, null, out _, sortings);
		}

		public object Get<TKey>(TKey key, object state, params Sorting[] sortings)
		{
			return this.Get<TKey>(key, string.Empty, null, state, out _, sortings);
		}

		public object Get<TKey>(TKey key, Paging paging, params Sorting[] sortings)
		{
			return this.Get<TKey>(key, string.Empty, paging, null, out _, sortings);
		}

		public object Get<TKey>(TKey key, string schema, params Sorting[] sortings)
		{
			return this.Get<TKey>(key, schema, null, null, out _, sortings);
		}

		public object Get<TKey>(TKey key, string schema, object state, params Sorting[] sortings)
		{
			return this.Get<TKey>(key, schema, null, state, out _, sortings);
		}

		public object Get<TKey>(TKey key, string schema, Paging paging, params Sorting[] sortings)
		{
			return this.Get<TKey>(key, schema, paging, null, out _, sortings);
		}

		public object Get<TKey>(TKey key, string schema, Paging paging, object state, params Sorting[] sortings)
		{
			return this.Get(key, schema, paging, state, out _, sortings);
		}

		public object Get<TKey>(TKey key, string schema, Paging paging, object state, out IPaginator paginator, params Sorting[] sortings)
		{
			var condition = this.ConvertKey(key, out var singleton);

			if(singleton)
			{
				//进行授权验证
				this.Authorize(Method.Get(), state);

				//修整查询条件
				condition = this.OnValidate(Method.Get(), condition);

				//执行单条查询方法
				return this.OnGet(condition, this.GetSchema(schema), state, out paginator);
			}

			var result = this.Select(condition, schema, paging, state, sortings);
			paginator = result as IPaginator;
			return result;
		}
		#endregion

		#region 双键查询
		public object Get<TKey1, TKey2>(TKey1 key1, TKey2 key2, params Sorting[] sortings)
		{
			return this.Get<TKey1, TKey2>(key1, key2, string.Empty, null, null, out _, sortings);
		}

		public object Get<TKey1, TKey2>(TKey1 key1, TKey2 key2, object state, params Sorting[] sortings)
		{
			return this.Get<TKey1, TKey2>(key1, key2, string.Empty, null, state, out _, sortings);
		}

		public object Get<TKey1, TKey2>(TKey1 key1, TKey2 key2, Paging paging, params Sorting[] sortings)
		{
			return this.Get<TKey1, TKey2>(key1, key2, string.Empty, paging, null, out _, sortings);
		}

		public object Get<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, params Sorting[] sortings)
		{
			return this.Get<TKey1, TKey2>(key1, key2, schema, null, null, out _, sortings);
		}

		public object Get<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, object state, params Sorting[] sortings)
		{
			return this.Get<TKey1, TKey2>(key1, key2, schema, null, state, out _, sortings);
		}

		public object Get<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, Paging paging, params Sorting[] sortings)
		{
			return this.Get<TKey1, TKey2>(key1, key2, schema, paging, null, out _, sortings);
		}

		public object Get<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, Paging paging, object state, params Sorting[] sortings)
		{
			return this.Get(key1, key2, schema, paging, state, out _, sortings);
		}

		public object Get<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, Paging paging, object state, out IPaginator paginator, params Sorting[] sortings)
		{
			var condition = this.ConvertKey(key1, key2, out var singleton);

			if(singleton)
			{
				//进行授权验证
				this.Authorize(Method.Get(), state);

				//修整查询条件
				condition = this.OnValidate(Method.Get(), condition);

				//执行单条查询方法
				return this.OnGet(condition, this.GetSchema(schema), state, out paginator);
			}

			var result = this.Select(condition, schema, paging, state, sortings);
			paginator = result as IPaginator;
			return result;
		}
		#endregion

		#region 三键查询
		public object Get<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, params Sorting[] sortings)
		{
			return this.Get(key1, key2, key3, string.Empty, null, null, out _, sortings);
		}

		public object Get<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, object state, params Sorting[] sortings)
		{
			return this.Get(key1, key2, key3, string.Empty, null, state, out _, sortings);
		}

		public object Get<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, Paging paging, params Sorting[] sortings)
		{
			return this.Get(key1, key2, key3, string.Empty, paging, null, out _, sortings);
		}

		public object Get<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, params Sorting[] sortings)
		{
			return this.Get(key1, key2, key3, schema, null, null, out _, sortings);
		}

		public object Get<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, object state, params Sorting[] sortings)
		{
			return this.Get(key1, key2, key3, schema, null, state, out _, sortings);
		}

		public object Get<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, Paging paging, params Sorting[] sortings)
		{
			return this.Get(key1, key2, key3, schema, paging, null, out _, sortings);
		}

		public object Get<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, Paging paging, object state, params Sorting[] sortings)
		{
			return this.Get(key1, key2, key3, schema, paging, state, out _, sortings);
		}

		public object Get<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, Paging paging, object state, out IPaginator paginator, params Sorting[] sortings)
		{
			var condition = this.ConvertKey(key1, key2, key3, out var singleton);

			if(singleton)
			{
				//进行授权验证
				this.Authorize(Method.Get(), state);

				//修整查询条件
				condition = this.OnValidate(Method.Get(), condition);

				//执行单条查询方法
				return this.OnGet(condition, this.GetSchema(schema), state, out paginator);
			}

			var result = this.Select(condition, schema, paging, state, sortings);
			paginator = result as IPaginator;
			return result;
		}

		protected virtual TEntity OnGet(ICondition condition, ISchema schema, object state, out IPaginator paginator)
		{
			var result = this.DataAccess.Select<TEntity>(this.Name, condition, schema, null, state, null, ctx => this.OnGetting(ctx), ctx => this.OnGetted(ctx));
			paginator = result as IPaginator;
			return result.FirstOrDefault();
		}
		#endregion

		#region 常规查询
		public IEnumerable<TEntity> Select(object state = null, params Sorting[] sortings)
		{
			return this.Select(null, string.Empty, null, state, sortings);
		}

		public IEnumerable<TEntity> Select(ICondition condition, params Sorting[] sortings)
		{
			return this.Select(condition, string.Empty, null, null, sortings);
		}

		public IEnumerable<TEntity> Select(ICondition condition, object state, params Sorting[] sortings)
		{
			return this.Select(condition, string.Empty, null, state, sortings);
		}

		public IEnumerable<TEntity> Select(ICondition condition, Paging paging, params Sorting[] sortings)
		{
			return this.Select(condition, string.Empty, paging, null, sortings);
		}

		public IEnumerable<TEntity> Select(ICondition condition, Paging paging, object state, params Sorting[] sortings)
		{
			return this.Select(condition, string.Empty, paging, state, sortings);
		}

		public IEnumerable<TEntity> Select(ICondition condition, Paging paging, string schema, params Sorting[] sortings)
		{
			return this.Select(condition, schema, paging, null, sortings);
		}

		public IEnumerable<TEntity> Select(ICondition condition, Paging paging, string schema, object state, params Sorting[] sortings)
		{
			return this.Select(condition, schema, paging, state, sortings);
		}

		public IEnumerable<TEntity> Select(ICondition condition, string schema, params Sorting[] sortings)
		{
			return this.Select(condition, schema, null, null, sortings);
		}

		public IEnumerable<TEntity> Select(ICondition condition, string schema, object state, params Sorting[] sortings)
		{
			return this.Select(condition, schema, null, state, sortings);
		}

		public IEnumerable<TEntity> Select(ICondition condition, string schema, Paging paging, params Sorting[] sortings)
		{
			return this.Select(condition, schema, paging, null, sortings);
		}

		public IEnumerable<TEntity> Select(ICondition condition, string schema, Paging paging, object state, params Sorting[] sortings)
		{
			//进行授权验证
			this.Authorize(Method.Select(), state);

			//修整查询条件
			condition = this.OnValidate(Method.Select(), condition);

			//执行查询方法
			return this.OnSelect(condition, this.GetSchema(schema, typeof(TEntity)), paging, sortings, state);
		}

		protected virtual IEnumerable<TEntity> OnSelect(ICondition condition, ISchema schema, Paging paging, Sorting[] sortings, object state)
		{
			return this.DataAccess.Select<TEntity>(this.Name, condition, schema, paging, state, sortings, ctx => this.OnSelecting(ctx), ctx => this.OnSelected(ctx));
		}
		#endregion

		#region 分组查询
		public IEnumerable<T> Select<T>(Grouping grouping, params Sorting[] sortings)
		{
			return this.Select<T>(grouping, null, string.Empty, null, null, sortings);
		}

		public IEnumerable<T> Select<T>(Grouping grouping, object state, params Sorting[] sortings)
		{
			return this.Select<T>(grouping, null, string.Empty, null, state, sortings);
		}

		public IEnumerable<T> Select<T>(Grouping grouping, Paging paging, object state = null, params Sorting[] sortings)
		{
			return this.Select<T>(grouping, null, string.Empty, paging, state, sortings);
		}

		public IEnumerable<T> Select<T>(Grouping grouping, string schema, params Sorting[] sortings)
		{
			return this.Select<T>(grouping, null, schema, null, null, sortings);
		}

		public IEnumerable<T> Select<T>(Grouping grouping, string schema, object state, params Sorting[] sortings)
		{
			return this.Select<T>(grouping, null, schema, null, state, sortings);
		}

		public IEnumerable<T> Select<T>(Grouping grouping, string schema, Paging paging, object state = null, params Sorting[] sortings)
		{
			return this.Select<T>(grouping, null, schema, paging, state, sortings);
		}

		public IEnumerable<T> Select<T>(Grouping grouping, ICondition condition, string schema, params Sorting[] sortings)
		{
			return this.Select<T>(grouping, condition, schema, null, null, sortings);
		}

		public IEnumerable<T> Select<T>(Grouping grouping, ICondition condition, string schema, object state, params Sorting[] sortings)
		{
			return this.Select<T>(grouping, condition, schema, null, state, sortings);
		}

		public IEnumerable<T> Select<T>(Grouping grouping, ICondition condition, string schema, Paging paging, object state = null, params Sorting[] sortings)
		{
			//进行授权验证
			this.Authorize(Method.Select(), state);

			//修整查询条件
			condition = this.OnValidate(Method.Select(), condition);

			//执行查询方法
			return this.OnSelect<T>(grouping, condition, this.GetSchema(schema, typeof(TEntity)), paging, sortings, state);
		}

		protected virtual IEnumerable<T> OnSelect<T>(Grouping grouping, ICondition condition, ISchema schema, Paging paging, Sorting[] sortings, object state)
		{
			return this.DataAccess.Select<T>(this.Name, grouping, condition, schema, paging, state, sortings, ctx => this.OnSelecting(ctx), ctx => this.OnSelected(ctx));
		}
		#endregion

		#region 显式实现
		IEnumerable IDataService.Select(object state, params Sorting[] sortings)
		{
			return this.Select(state, sortings);
		}

		IEnumerable IDataService.Select(ICondition condition, params Sorting[] sortings)
		{
			return this.Select(condition, sortings);
		}

		IEnumerable IDataService.Select(ICondition condition, object state, params Sorting[] sortings)
		{
			return this.Select(condition, state, sortings);
		}

		IEnumerable IDataService.Select(ICondition condition, string schema, params Sorting[] sortings)
		{
			return this.Select(condition, schema, sortings);
		}

		IEnumerable IDataService.Select(ICondition condition, string schema, object state, params Sorting[] sortings)
		{
			return this.Select(condition, schema, state, sortings);
		}

		IEnumerable IDataService.Select(ICondition condition, string schema, Paging paging, params Sorting[] sortings)
		{
			return this.Select(condition, schema, paging, sortings);
		}

		IEnumerable IDataService.Select(ICondition condition, string schema, Paging paging, object state, params Sorting[] sortings)
		{
			return this.Select(condition, schema, paging, state, sortings);
		}

		IEnumerable IDataService.Select(ICondition condition, Paging paging, params Sorting[] sortings)
		{
			return this.Select(condition, paging, sortings);
		}

		IEnumerable IDataService.Select(ICondition condition, Paging paging, object state, params Sorting[] sortings)
		{
			return this.Select(condition, paging, state, sortings);
		}

		IEnumerable IDataService.Select(ICondition condition, Paging paging, string schema, params Sorting[] sortings)
		{
			return this.Select(condition, paging, schema, sortings);
		}

		IEnumerable IDataService.Select(ICondition condition, Paging paging, string schema, object state, params Sorting[] sortings)
		{
			return this.Select(condition, paging, schema, state, sortings);
		}
		#endregion

		#endregion

		#region 校验方法
		protected virtual ICondition OnValidate(Method method, ICondition condition)
		{
			return condition;
		}

		protected virtual void OnValidate(Method method, IDataDictionary<TEntity> data)
		{
		}
		#endregion

		#region 激发事件
		protected virtual void OnGetted(DataSelectContextBase context)
		{
			var e = this.Getted;

			if(e != null)
				e(this, new DataGettedEventArgs<TEntity>(context));
		}

		protected virtual bool OnGetting(DataSelectContextBase context)
		{
			var e = this.Getting;

			if(e == null)
				return false;

			var args = new DataGettingEventArgs<TEntity>(context);
			e(this, args);
			return args.Cancel;
		}

		protected virtual void OnCounted(DataCountContextBase context)
		{
			var e = this.Counted;

			if(e != null)
				e(this, new DataCountedEventArgs(context));
		}

		protected virtual bool OnCounting(DataCountContextBase context)
		{
			var e = this.Counting;

			if(e == null)
				return false;

			var args = new DataCountingEventArgs(context);
			e(this, args);
			return args.Cancel;
		}

		protected virtual void OnExecuted(DataExecuteContextBase context)
		{
			var e = this.Executed;

			if(e != null)
				e(this, new DataExecutedEventArgs(context));
		}

		protected virtual bool OnExecuting(DataExecuteContextBase context)
		{
			var e = this.Executing;

			if(e == null)
				return false;

			var args = new DataExecutingEventArgs(context);
			e(this, args);
			return args.Cancel;
		}

		protected virtual void OnExisted(DataExistContextBase context)
		{
			var e = this.Existed;

			if(e != null)
				e(this, new DataExistedEventArgs(context));
		}

		protected virtual bool OnExisting(DataExistContextBase context)
		{
			var e = this.Existing;

			if(e == null)
				return false;

			var args = new DataExistingEventArgs(context);
			e(this, args);
			return args.Cancel;
		}

		protected virtual void OnIncremented(DataIncrementContextBase context)
		{
			var e = this.Incremented;

			if(e != null)
				e(this, new DataIncrementedEventArgs(context));
		}

		protected virtual bool OnIncrementing(DataIncrementContextBase context)
		{
			var e = this.Incrementing;

			if(e == null)
				return false;

			var args = new DataIncrementingEventArgs(context);
			e(this, args);
			return args.Cancel;
		}

		protected virtual void OnDeleted(DataDeleteContextBase context)
		{
			var e = this.Deleted;

			if(e != null)
				e(this, new DataDeletedEventArgs(context));
		}

		protected virtual bool OnDeleting(DataDeleteContextBase context)
		{
			var e = this.Deleting;

			if(e == null)
				return false;

			var args = new DataDeletingEventArgs(context);
			e(this, args);
			return args.Cancel;
		}

		protected virtual void OnInserted(DataInsertContextBase context)
		{
			var e = this.Inserted;

			if(e != null)
				e(this, new DataInsertedEventArgs(context));
		}

		protected virtual bool OnInserting(DataInsertContextBase context)
		{
			var e = this.Inserting;

			if(e == null)
				return false;

			var args = new DataInsertingEventArgs(context);
			e(this, args);
			return args.Cancel;
		}

		protected virtual void OnUpdated(DataUpdateContextBase context)
		{
			var e = this.Updated;

			if(e != null)
				e(this, new DataUpdatedEventArgs(context));
		}

		protected virtual bool OnUpdating(DataUpdateContextBase context)
		{
			var e = this.Updating;

			if(e == null)
				return false;

			var args = new DataUpdatingEventArgs(context);
			e(this, args);
			return args.Cancel;
		}

		protected virtual void OnSelected(DataSelectContextBase context)
		{
			var e = this.Selected;

			if(e != null)
				e(this, new DataSelectedEventArgs(context));
		}

		protected virtual bool OnSelecting(DataSelectContextBase context)
		{
			var e = this.Selecting;

			if(e == null)
				return false;

			var args = new DataSelectingEventArgs(context);
			e(this, args);
			return args.Cancel;
		}
		#endregion

		#region 键值操作
		/// <summary>
		/// 根据指定的查询参数值获取对应的查询<see cref="ICondition"/>条件。
		/// </summary>
		/// <param name="values">指定的查询值数组。</param>
		/// <param name="singleton">输出一个值，指示返回的查询条件执行后的结果是否为单个对象。</param>
		/// <returns>返回对应的查询<see cref="ICondition"/>条件。</returns>
		/// <remarks>
		///		<para>基类的实现始终返回当前数据服务对应的主键的键值对数组。</para>
		/// </remarks>
		protected virtual ICondition GetKey(object[] values, out bool singleton)
		{
			//设置输出参数默认值
			singleton = false;

			if(values == null || values.Length == 0)
				return null;

			//获取当前数据服务对应的主键
			var primaryKey = this.DataAccess.GetKey(this.Name);

			//如果主键获取失败或主键未定义或主键项数量不等于传入的数组元素个数则返回空
			if(primaryKey == null || primaryKey.Length == 0 || primaryKey.Length != values.Length)
				return null;

			//匹配主键，故查询结果为单一项
			singleton = true;

			//如果主键成员只有一个则返回单个条件
			if(primaryKey.Length == 1)
			{
				if(values[0] is string text && text != null && text.Length > 0)
				{
					var parts = text.Split(',').Select(p => p.Trim()).Where(p => p.Length > 0).ToArray();

					if(parts.Length > 1)
					{
						singleton = false;
						return Condition.In(primaryKey[0], parts);
					}

					return Condition.Equal(primaryKey[0], parts[0]);
				}

				return Condition.Equal(primaryKey[0], values[0]);
			}

			//创建返回的条件集（AND组合）
			var conditions = ConditionCollection.And();

			for(int i = 0; i < primaryKey.Length; i++)
			{
				conditions.Add(Data.Condition.Equal(primaryKey[i], values[i]));
			}

			return conditions;
		}
		#endregion

		#region 私有方法
		private ICondition ConvertKey<TKey>(TKey key, out bool singleton)
		{
			return this.EnsureInquiryKey(new object[] { key }, out singleton);
		}

		private ICondition ConvertKey<TKey1, TKey2>(TKey1 key1, TKey2 key2, out bool singleton)
		{
			return this.EnsureInquiryKey(new object[] { key1, key2 }, out singleton);
		}

		private ICondition ConvertKey<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, out bool singleton)
		{
			return this.EnsureInquiryKey(new object[] { key1, key2, key3 }, out singleton);
		}

		private ICondition EnsureInquiryKey(object[] values, out bool singleton)
		{
			if(values != null && values.Length > 3)
				throw new NotSupportedException("Too many the keys.");

			//获取查询键值对数组
			var condition = this.GetKey(values ?? new object[0], out singleton);

			if(condition == null)
				throw new ArgumentException($"The specified key is invalid of the {this.Name} service.");

			if(condition != null && condition is ConditionCollection)
			{
				if(((ConditionCollection)condition).Count == 1)
					return ((ConditionCollection)condition)[0];
			}

			return condition;
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private ISchema GetSchema(string expression, Type type = null)
		{
			return this.DataAccess.Schema.Parse(this.Name, expression, type ?? typeof(TEntity));
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private ICondition EnsureUpdateCondition(IDataDictionary dictionary)
		{
			var keys = this.DataAccess.GetKey(this.Name);
			var requires = new ICondition[keys.Length];

			for(int i = 0; i < keys.Length; i++)
			{
				if(dictionary.TryGetValue(keys[i], out var value) && value != null)
					requires[i] = Condition.Equal(keys[i], value);
				else
					throw new DataException($"No required primary key field value is specified for the update '{this.Name}' entity data.");
			}

			return requires.Length > 1 ? ConditionCollection.And(requires) : requires[0];
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private void EnsureDelete()
		{
			if(!this.CanDelete)
				throw new InvalidOperationException("The delete operation is not allowed.");
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private void EnsureInsert()
		{
			if(!this.CanInsert)
				throw new InvalidOperationException("The insert operation is not allowed.");
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private void EnsureUpdate()
		{
			if(!this.CanUpdate)
				throw new InvalidOperationException("The update operation is not allowed.");
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private void EnsureUpsert()
		{
			if(!this.CanUpsert)
				throw new InvalidOperationException("The upsert operation is not allowed.");
		}
		#endregion

		#region 嵌套子类
		protected struct Method : IEquatable<Method>
		{
			#region 公共字段
			/// <summary>方法的名称。</summary>
			public readonly string Name;

			/// <summary>对应的数据访问方法种类。</summary>
			public readonly DataAccessMethod Kind;
			#endregion

			#region 构造函数
			private Method(DataAccessMethod kind)
			{
				this.Kind = kind;
				this.Name = kind.ToString();
			}

			private Method(string name, DataAccessMethod kind)
			{
				this.Name = name ?? kind.ToString();
				this.Kind = kind;
			}
			#endregion

			#region 静态方法
			public static Method Get()
			{
				return new Method(nameof(Get), DataAccessMethod.Select);
			}

			public static Method Count()
			{
				return new Method(DataAccessMethod.Count);
			}

			public static Method Exists()
			{
				return new Method(DataAccessMethod.Exists);
			}

			public static Method Execute()
			{
				return new Method(DataAccessMethod.Execute);
			}

			public static Method Increment()
			{
				return new Method(nameof(Increment), DataAccessMethod.Increment);
			}

			public static Method Decrement()
			{
				return new Method(nameof(Decrement), DataAccessMethod.Increment);
			}

			public static Method Select(string name = null)
			{
				if(string.IsNullOrEmpty(name))
					return new Method(DataAccessMethod.Select);
				else
					return new Method(name, DataAccessMethod.Select);
			}

			public static Method Delete()
			{
				return new Method(DataAccessMethod.Delete);
			}

			public static Method Insert()
			{
				return new Method(DataAccessMethod.Insert);
			}

			public static Method InsertMany()
			{
				return new Method(nameof(InsertMany), DataAccessMethod.Insert);
			}

			public static Method Update()
			{
				return new Method(DataAccessMethod.Update);
			}

			public static Method UpdateMany()
			{
				return new Method(nameof(UpdateMany), DataAccessMethod.Update);
			}

			public static Method Upsert()
			{
				return new Method(DataAccessMethod.Upsert);
			}

			public static Method UpsertMany()
			{
				return new Method(nameof(UpsertMany), DataAccessMethod.Upsert);
			}
			#endregion

			#region 公共方法
			/// <summary>
			/// 获取一个值，指示当前方法是否为读取方法(Count/Exists/Select)。
			/// </summary>
			public bool IsReading
			{
				get
				{
					return this.Kind == DataAccessMethod.Count ||
						this.Kind == DataAccessMethod.Exists ||
						this.Kind == DataAccessMethod.Select;
				}
			}

			/// <summary>
			/// 获取一个值，指示当前方法是否为修改方法(Incremnet/Decrement/Delete/Insert/Update/Upsert)。
			/// </summary>
			public bool IsWriting
			{
				get
				{
					return this.Kind == DataAccessMethod.Increment || 
						this.Kind == DataAccessMethod.Delete ||
						this.Kind == DataAccessMethod.Insert ||
						this.Kind == DataAccessMethod.Update ||
						this.Kind == DataAccessMethod.Upsert;
				}
			}
			#endregion

			#region 重写方法
			public bool Equals(Method method)
			{
				return this.Kind == method.Kind && string.Equals(this.Name, method.Name);
			}

			public override bool Equals(object obj)
			{
				if(obj == null || obj.GetType() != typeof(Method))
					return false;

				return this.Equals((Method)obj);
			}

			public override int GetHashCode()
			{
				return this.Name.GetHashCode();
			}

			public override string ToString()
			{
				return this.Name;
			}
			#endregion
		}

		public sealed class Condition : Zongsoft.Data.Condition.Builder<TEntity>
		{
			#region 私有构造
			private Condition()
			{
			}
			#endregion
		}

		private sealed class InnerDataSearcher : DataSearcher<TEntity>
		{
			public InnerDataSearcher(DataServiceBase<TEntity> dataService, DataSearcherAttribute[] attributes) : base(dataService, attributes)
			{
			}

			protected override ICondition Resolve(string method, string keyword, object state = null)
			{
				switch(method)
				{
					case nameof(this.Count):
						return ((DataServiceBase<TEntity>)this.DataService).OnValidate(Method.Count(), base.Resolve(method, keyword, state));
					case nameof(this.Exists):
						return ((DataServiceBase<TEntity>)this.DataService).OnValidate(Method.Exists(), base.Resolve(method, keyword, state));
					case nameof(this.Search):
						return ((DataServiceBase<TEntity>)this.DataService).OnValidate(Method.Select(method), base.Resolve(method, keyword, state));
				}

				return base.Resolve(method, keyword, state);
			}
		}
		#endregion
	}
}
