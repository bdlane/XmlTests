using Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using TransactionServiceExtensions;

namespace ConsoleApp1
{
    class Program
    {
        // GenerateDeserializer method
        // Or just the constructor, using index and known types.
        private static Dictionary<Type, TypeConverter> typeConverters = new Dictionary<Type, TypeConverter>();

        static Action<List<string>> PrintList(List<string> args)
        {
            var paramListExp = Expression.Parameter(typeof(List<string>), "array");
            var consoleWriteMethod = typeof(Console).GetMethod(nameof(Console.WriteLine), new[] { typeof(string) });

            var propInfo = typeof(List<string>).GetProperty("Item");

            var x = args.Select((a, i) =>
            {
                var indexed = Expression.Property(paramListExp, propInfo, Expression.Constant(i));
                var writeMethodCallExp = Expression.Call(consoleWriteMethod, new[] { indexed });

                return writeMethodCallExp;
            });

            var y = Expression.Block(x);

            return Expression.Lambda<Action<List<string>>>(y, new[] { paramListExp }).Compile();
        }

        static Func<List<string>, T> GenerateConstructorMethod<T>()
        {
            // Assume list = ctor param order and types
            // Assume T is an class with a specialized constructor

            var type = typeof(T);
            var ctor = type.GetConstructors().Single();

            var propInfo = typeof(List<string>).GetProperty("Item");
            var paramListExp = Expression.Parameter(typeof(List<string>), "args");

            var argExps = ctor.GetParameters().Select((p, i) =>
            {
                var paramType = p.ParameterType;
                // Get converter (TODO: cache)
                var itemPropExp = Expression.Property(paramListExp, propInfo, Expression.Constant(i));

                var getConverterExp = Expression.Call(
                    typeof(TypeDescriptor).GetMethod(nameof(TypeDescriptor.GetConverter), new[] { typeof(Type) }),
                    Expression.Constant(paramType));

                var convertFromStringExp = Expression.Call(
                    getConverterExp,
                    typeof(TypeConverter).GetMethod(nameof(TypeConverter.ConvertFromInvariantString), new[] { typeof(string) }),
                    new[] { itemPropExp });

                var castExp = Expression.Convert(convertFromStringExp, paramType);

                return castExp;

                // Convert
                // Cast
            });

            var newExp = Expression.New(ctor, argExps);

            // Construct
            // Return

            return Expression.Lambda<Func<List<string>, T>>(newExp, paramListExp).Compile();
        }

        static Func<T> GeneratePropMethod<T>()
        {
            // Assume list = ctor param order and types
            // Assume T is an class with a specialized constructor

            var type = typeof(T);

            var initExpression = Expression.MemberInit(
                Expression.New(type),
                Expression.Bind(type.GetProperty("Name"), Expression.Constant("Bob")),
                Expression.Bind(type.GetProperty("Age"), Expression.Constant(25)));

            return Expression.Lambda<Func<T>>(initExpression).Compile();
        }

        static Func<List<string>, T> GeneratePropMethodDynamic<T>(List<string> fields)
        {
            // Assume field list will be same order as values list
            // Assume T is an class with a default constructor

            var type = typeof(T);

            var paramListExp = Expression.Parameter(typeof(List<string>), "fieldValues");

            var initializerExps = fields.Select((f, i) =>
            {
                var itemPropExp = Expression.Property(
                    paramListExp, 
                    typeof(List<string>).GetProperty("Item"), 
                    Expression.Constant(i));

                // Ignore casing for now
                var propInfo = type.GetProperty(f);

                var getConverterExp = Expression.Call(
                    typeof(TypeDescriptor).GetMethod(nameof(TypeDescriptor.GetConverter), new[] { typeof(Type) }),
                    Expression.Constant(propInfo.PropertyType));

                var convertFromStringExp = Expression.Call(
                    getConverterExp,
                    typeof(TypeConverter).GetMethod(nameof(TypeConverter.ConvertFromInvariantString), new[] { typeof(string) }),
                    new[] { itemPropExp });

                var castExp = Expression.Convert(convertFromStringExp, propInfo.PropertyType);

                return Expression.Bind(
                    propInfo,
                    castExp);
            });

            var initExpression = Expression.MemberInit(
                Expression.New(type),
                initializerExps);

            return Expression.Lambda<Func<List<string>, T>>(initExpression, paramListExp).Compile();
        }

        static void Main(string[] args)
        {
            //MethodCall();

            //IndexIntoArray();

            var propMethod = GeneratePropMethodDynamic<TimekeeperSimpleProp>(new List<string> { "Name", "Age" });

            var tProp = propMethod(new List<string> { "Bob", "25" });

            var fields = new List<string>
            {
                "Bob",
                "25"
            };

            var e = GenerateConstructorMethod<TimekeeperSimpleCtor>();

            var t = e(fields);


            var l = PrintList(fields);

            l(fields);

            var paramListExp = Expression.Parameter(typeof(List<string>), "array");
            var varNameExp = Expression.Variable(typeof(string), "name");

            var ctor = typeof(TimekeeperSimpleCtor).GetConstructors().First();

            var propInfo = typeof(List<string>).GetProperty("Item");
            //var property = Expression.Property(paramListExp, "Item",);
            var indexed = Expression.Property(paramListExp, propInfo, Expression.Constant(0));
            var assignExp = Expression.Assign(varNameExp, indexed);

            var consoleWriteMethod = typeof(Console).GetMethod(nameof(Console.WriteLine), new[] { typeof(string) });

            var writeMethodCallExp = Expression.Call(consoleWriteMethod, new[] { varNameExp });

            var blockExp = Expression.Block(
                new[] { varNameExp },
                assignExp,
                writeMethodCallExp);

            var compiled = Expression.Lambda<Action<List<string>>>(blockExp, paramListExp).Compile();

            compiled(new List<string> { "Bob", "James" });


            //ctor.GetParameters().Select((p, i) =>
            //{
            //    var indexed = Expression.Property(property, "Item", Expression.Constant(0));
            //    var paramVarExp = Expression.Variable(p.ParameterType, p.Name);

            //});

            //var newExp = Expression.New(ctor, varNameExp);

            //var l = Expression.Lambda<Func<object[], Timekeeper>>(newExp, varNameExp);
            //var comp = l.Compile();

            //comp(new object[] { "bob" });

            Console.ReadKey();
        }

        private static void IndexIntoArray()
        {
            var paramArrayExp = Expression.Parameter(typeof(string[]), "array");
            var varNameExp = Expression.Variable(typeof(string), "name");

            var indexExp = Expression.ArrayIndex(paramArrayExp, Expression.Constant(2));
            var assignExp = Expression.Assign(varNameExp, indexExp);

            var consoleWriteMethod = typeof(Console).GetMethod(nameof(Console.WriteLine), new[] { typeof(string) });

            var writeMethodCallExp = Expression.Call(consoleWriteMethod, new[] { varNameExp });
            var blockExp = Expression.Block(new[] { varNameExp }, assignExp, writeMethodCallExp);

            var compiled = Expression.Lambda<Action<string[]>>(blockExp, paramArrayExp).Compile();

            compiled(new[] { "Bob", "James" });
        }

        private static void MethodCall()
        {
            var sw = Stopwatch.StartNew();
            var p = Expression.Parameter(typeof(string), "name");

            var m = typeof(Program).GetMethod(
                nameof(SayHello),
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);

            var call = Expression.Call(m, p);
            var exp = Expression.Lambda<Action<string>>(call, p);

            Console.WriteLine($"Created expression: {sw.Elapsed}");
            sw.Restart();

            var sayHelloDelegate = exp.Compile();

            Console.WriteLine($"Compiled expression: {sw.Elapsed}");
            sw.Restart();

            sayHelloDelegate("Bob");

            Console.WriteLine($"Called expression: {sw.Elapsed}");
            sw.Restart();

            sayHelloDelegate("Fred");

            Console.WriteLine($"Called expression: {sw.Elapsed}");
            sw.Restart();
        }

        static void SayHello(string name)
        {
            Console.WriteLine($"Hello {name}");
        }
    }

    class MockService : ITransactionService
    {
        public string GetArchetypeData(string queryXml) => "<Data><Timekeeper><Name>Bob</Name></Timekeeper></Data>";
    }
}
