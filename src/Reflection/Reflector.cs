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
 * Copyright (C) 2010-2018 Zongsoft Corporation <http://www.zongsoft.com>
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
using System.Reflection;

namespace Zongsoft.Reflection
{
	public static class Reflector
	{
		public static object GetValue(this MemberInfo member, ref object target, params object[] parameters)
		{
			if(member == null)
				throw new ArgumentNullException(nameof(member));

			switch(member.MemberType)
			{
				case MemberTypes.Field:
					return GetValue((FieldInfo)member, ref target);
				case MemberTypes.Property:
					return GetValue((PropertyInfo)member, ref target, parameters);
				default:
					throw new NotSupportedException($"The {member.MemberType.ToString()} of member that is not supported.");
			}
		}

		public static object GetValue(this FieldInfo field, ref object target)
		{
			return field.GetGetter().Invoke(ref target);
		}

		public static object GetValue(this PropertyInfo property, ref object target, params object[] parameters)
		{
			if(!property.CanRead)
				throw new InvalidOperationException($"The '{property.Name}' property does not support reading.");

			return property.GetGetter().Invoke(ref target, parameters);
		}

		public static object GetValue(object target, string name, params object[] parameters)
		{
			if(target == null)
				throw new ArgumentNullException(nameof(target));

			var type = (target as Type) ?? target.GetType();
			var members = string.IsNullOrEmpty(name) ?
				type.GetDefaultMembers() :
				type.GetMember(name, MemberTypes.Property | MemberTypes.Field, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

			if(members == null || members.Length == 0)
				throw new ArgumentException($"A member named '{name}' does not exist in the '{type.FullName}' type.");

			return GetValue(members[0], ref target, parameters);
		}

		public static void SetValue(this MemberInfo member, ref object target, object value, params object[] parameters)
		{
			if(member == null)
				throw new ArgumentNullException(nameof(member));

			switch(member.MemberType)
			{
				case MemberTypes.Field:
					SetValue((FieldInfo)member, ref target, value);
					break;
				case MemberTypes.Property:
					SetValue((PropertyInfo)member, ref target, value, parameters);
					break;
				default:
					throw new NotSupportedException($"The {member.MemberType.ToString()} of member that is not supported.");
			}
		}

		public static void SetValue(this FieldInfo field, ref object target, object value)
		{
			field.GetSetter().Invoke(ref target, value);
		}

		public static void SetValue(this PropertyInfo property, ref object target, object value, params object[] parameters)
		{
			if(!property.CanWrite)
				throw new InvalidOperationException($"The '{property.Name}' property does not support writing.");

			property.GetSetter().Invoke(ref target, value, parameters);
		}

		public static void SetValue(ref object target, string name, object value, params object[] parameters)
		{
			if(target == null)
				throw new ArgumentNullException(nameof(target));

			var type = (target as Type) ?? target.GetType();
			var members = string.IsNullOrEmpty(name) ?
				type.GetDefaultMembers() :
				type.GetMember(name, MemberTypes.Property | MemberTypes.Field, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

			if(members == null || members.Length == 0)
				throw new ArgumentException($"A member named '{name}' does not exist in the '{type.FullName}' type.");

			SetValue(members[0], ref target, value, parameters);
		}
	}
}
