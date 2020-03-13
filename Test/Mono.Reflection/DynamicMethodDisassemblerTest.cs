using System;
using System.Reflection.Emit;

using NUnit.Framework;

namespace Mono.Reflection
{
    [TestFixture]
    public class DynamicMethodDisassemblerTest {

        [Test]
        public void Nop() {
            var dm = new DynamicMethod(nameof(Nop), typeof(void), new Type[0]);
            var ilg = dm.GetILGenerator();
            ilg.Emit(OpCodes.Nop);
            dm.CreateDelegate(typeof(Action));

            AssertMethod(@"
    IL_0000: nop
            ", dm);
        }

        [Test]
        public void OneLocal_NoInit()
        {
            var dm = new DynamicMethod(nameof(OneLocal_NoInit), 
                typeof(int), new Type[] { typeof(int) });

            var ilg = dm.GetILGenerator();
            var loc0 = ilg.DeclareLocal(typeof(int));

            ilg.Emit(OpCodes.Ldarg_0);
            ilg.Emit(OpCodes.Stloc, loc0);
            ilg.Emit(OpCodes.Ldloc, loc0);
            ilg.Emit(OpCodes.Ret);

            dm.CreateDelegate(typeof(Func<int, int>));

            AssertMethod(@"
    IL_0000: ldarg.0
    IL_0001: stloc.0
    IL_0002: ldloc.0
    IL_0003: ret
            ", dm);
        }

        [Test]
        public void OneLocal_WithInit() {
            var dm = new DynamicMethod(nameof(OneLocal_WithInit),
                typeof(int), new Type[] { typeof(int) });
            
            dm.InitLocals = true;
            
            var ilg = dm.GetILGenerator();
            var loc0 = ilg.DeclareLocal(typeof(int));

            ilg.Emit(OpCodes.Ldarg_0);
            ilg.Emit(OpCodes.Stloc, loc0);
            ilg.Emit(OpCodes.Ldloc, loc0);
            ilg.Emit(OpCodes.Ret);

            dm.CreateDelegate(typeof(Func<int, int>));

            AssertMethod(@"
    IL_0000: ldarg.0
    IL_0001: stloc.0
    IL_0002: ldloc.0
    IL_0003: ret
            ", dm);
        }


        static void AssertMethod(string code, DynamicMethod method)
        {
            Assert.AreEqual(Normalize(code), 
                Normalize(Formatter.FormatMethodBody(method)));
        }

        static readonly System.Text.RegularExpressions.Regex _regex
            = new System.Text.RegularExpressions.Regex(
                @"\s+");

        static string Normalize(string str) {
            return _regex.Replace(str.Trim(), ";");
        }

    }
}
