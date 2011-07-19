using System;
using System.Reflection;
using System.Reflection.Emit;

namespace mAdcOW.SharePoint.Search
{
    /// <summary>
    /// Helper class used for setting the core results webpart in fql mode
    /// Used for building fql with the correct data types
    ///
    /// Author: Mikael Svenson - mAdcOW deZign    
    /// E-mail: miksvenson@gmail.com
    /// Twitter: @mikaelsvenson
    /// 
    /// This source code is released under the MIT license
    /// </summary>
    public class DynamicReflectionHelperforObject<TV>
    {
        public delegate T GetPropertyFieldDelegate<T>(TV obj);

        public static GetPropertyFieldDelegate<TC> GetProperty<TC>(string memberName)
        {
            Type v = typeof (TV);
            PropertyInfo pi = v.GetProperty(memberName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (pi == null)
                throw new NullReferenceException("No Property or Field");

            DynamicMethod dm = new DynamicMethod("GetPropertyorField_" + memberName, typeof (TC), new[] {v}, v.Module);
            ILGenerator il = dm.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0); // loaded c, c is the return value
            il.EmitCall(OpCodes.Call, pi.GetGetMethod(true), null);
            il.Emit(OpCodes.Ret);
            return (GetPropertyFieldDelegate<TC>) dm.CreateDelegate(typeof (GetPropertyFieldDelegate<TC>));
        }
    }
}