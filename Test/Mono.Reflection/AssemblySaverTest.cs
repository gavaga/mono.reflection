using System;
using System.IO;
using System.Linq;

using SR = System.Reflection;
using SRE = System.Reflection.Emit;

using Mono.Cecil;
using Mono.Cecil.Cil;

using NUnit.Framework;

namespace Mono.Reflection {

	[TestFixture]
	public class AssemblySaverTest : BaseReflectionTest {

		[Test]
		public void SimpleType ()
		{
			var module = Save ((a, m) => m.DefineType ("Foo.Bar").CreateType ());

			var type = module.GetType ("Foo.Bar");
			Assert.IsNotNull (type);
		}

		[Test]
		public void SimpleMethod ()
		{
			var module = Save ((a, m) => {
				var type = m.DefineType ("Foo.Bar");

				var method = type.DefineMethod ("Baz", SR.MethodAttributes.Public | SR.MethodAttributes.Static, typeof (object), new [] { typeof (object) });
				var il = method.GetILGenerator ();
				il.Emit (SRE.OpCodes.Ldarg_0);
				il.Emit (SRE.OpCodes.Ret);

				type.CreateType ();
			});

			var bar = module.GetType ("Foo.Bar");
			var baz = bar.Methods.Single (m => m.Name == "Baz");

			Assert.AreEqual (2, baz.Body.Instructions.Count);
			Assert.AreEqual (OpCodes.Ldarg_0, baz.Body.Instructions [0].OpCode);
			Assert.AreEqual (OpCodes.Ret, baz.Body.Instructions [1].OpCode);
		}

		[Test]
		public void MethodWithVariable ()
		{
			var module = Save ((a, m) => {
				var type = m.DefineType ("Foo.Bar", SR.TypeAttributes.Public);

				var method = type.DefineMethod ("Baz", SR.MethodAttributes.Public, typeof (object), new [] { typeof (object) });
				var il = method.GetILGenerator ();
				var o = il.DeclareLocal (typeof (object));
				il.Emit (SRE.OpCodes.Ldarg_1);
				il.Emit (SRE.OpCodes.Stloc, o);
				il.Emit (SRE.OpCodes.Ldloc, o);
				il.Emit (SRE.OpCodes.Ret);

				type.CreateType ();
			});

			var bar = module.GetType ("Foo.Bar");
			var baz = bar.Methods.Single (m => m.Name == "Baz");

			Assert.AreEqual (4, baz.Body.Instructions.Count);

			Load (module, a => {
				dynamic b = Activator.CreateInstance (a.GetType ("Foo.Bar"));

				Assert.AreEqual (42, b.Baz (42));
			});
		}

		[Test]
		public void SimpleProperty ()
		{
			var module = Save ((a, m) => {
				var type = m.DefineType ("Foo.Bar", SR.TypeAttributes.Public);

				var field = type.DefineField ("baz", typeof (int), SR.FieldAttributes.Assembly);

				var property = type.DefineProperty ("Baz", SR.PropertyAttributes.None, typeof (int), Type.EmptyTypes);

				var set_Baz = type.DefineMethod ("set_Baz", SR.MethodAttributes.Public, typeof (void), new[] { typeof (int) });
				var il = set_Baz.GetILGenerator();
				il.Emit (SRE.OpCodes.Ldarg_0);
				il.Emit (SRE.OpCodes.Ldarg_1);
				il.Emit (SRE.OpCodes.Stfld, field);
				il.Emit (SRE.OpCodes.Ret);

				var get_Baz = type.DefineMethod ("get_Baz", SR.MethodAttributes.Public, typeof (int), Type.EmptyTypes);
				il = get_Baz.GetILGenerator ();
				il.Emit (SRE.OpCodes.Ldarg_0);
				il.Emit (SRE.OpCodes.Ldfld, field);
				il.Emit (SRE.OpCodes.Ret);

				property.SetSetMethod (set_Baz);
				property.SetGetMethod (get_Baz);

				type.CreateType ();
			});

			var bar = module.GetType ("Foo.Bar");
			var baz = bar.Properties.Single(m => m.Name == "Baz");

			Assert.AreEqual ("set_Baz", baz.SetMethod.Name);
			Assert.AreEqual ("get_Baz", baz.GetMethod.Name);

			Load (module, a => {
				dynamic b = Activator.CreateInstance (a.GetType ("Foo.Bar"));
				b.Baz = 42;

				Assert.AreEqual (42, b.Baz);
			});
		}

		private static ModuleDefinition Save (Action<SRE.AssemblyBuilder, SRE.ModuleBuilder> definer)
		{
			var name = "Save-" + Guid.NewGuid ().ToString ("D");
			var assembly = AppDomain.CurrentDomain.DefineDynamicAssembly (new SR.AssemblyName (name), SRE.AssemblyBuilderAccess.Run);
			var module = assembly.DefineDynamicModule (name);

			definer (assembly, module);

			var memory = new MemoryStream ();
			assembly.SaveTo (memory);

			return ModuleDefinition.ReadModule (new MemoryStream (memory.ToArray ()));
		}

		private static void Load (ModuleDefinition module, Action<SR.Assembly> asserter)
		{
			module.Name += "Loaded";
			module.Assembly.Name.Name += "Loaded";

			var memory = new MemoryStream ();
			module.Write (memory);

			var assembly = SR.Assembly.Load (memory.ToArray ());
			asserter (assembly);
		}
	}
}