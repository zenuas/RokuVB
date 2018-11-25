Imports System
Imports System.Diagnostics
Imports System.Collections.Generic
Imports System.Reflection
Imports System.Reflection.Emit
Imports Roku.Manager
Imports Roku.Manager.SystemLibrary
Imports Roku.Operator
Imports Roku.IntermediateCode
Imports Roku.Util.Extensions


Namespace Architecture

    Public Class CommonIL

        Public Overridable Property [Module] As ModuleBuilder

        Public Overridable Sub Assemble(root As SystemLibrary, entrypoint As RkNamespace, path As String, subsystem As PEFileKinds)

            Dim name As New AssemblyName(entrypoint.Name)
            Dim asm = System.AppDomain.CurrentDomain.DefineDynamicAssembly(name, AssemblyBuilderAccess.Save)
            Me.Module = asm.DefineDynamicModule(entrypoint.Name, System.IO.Path.GetRandomFileName, True)

            Dim structs = Me.DeclareStructs(root)
            Dim functions = Me.DeclareMethods(root, structs)
            Me.DeclareStatements(root, functions, structs)
            structs.Each(Sub(x) If TypeOf x.Value.Type Is TypeBuilder Then CType(x.Value.Type, TypeBuilder).CreateType())
            Me.Binders.Each(Sub(x) CType(x.Value.Type, TypeBuilder).CreateType())

            If subsystem <> PEFileKinds.Dll Then

                ' global sub main() {entrypoint.###.ctor();}
                Dim ctor = entrypoint.FindCurrentFunction(".ctor").First
                If ctor IsNot Nothing Then asm.SetEntryPoint(functions(ctor), subsystem)
            End If
            Me.Module.CreateGlobalFunctions()

            Dim temp = System.IO.Path.GetFileName(Me.Module.FullyQualifiedName)
            Dim pdb = System.IO.Path.GetFileNameWithoutExtension(temp) + ".pdb"
            Try
                asm.Save(temp)
                System.IO.File.Copy(temp, path, True)
                System.IO.File.Copy(temp, System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path), System.IO.Path.GetFileNameWithoutExtension(path) + ".pdb"), True)

            Finally
                System.IO.File.Delete(temp)
                System.IO.File.Delete(pdb)

            End Try
        End Sub

        Public Overridable Function ConvertValidName(s As String) As String

            Return If(System.Text.RegularExpressions.Regex.IsMatch(s, "^_*[a-zA-Z]+[_0-9a-zA-Z]*$"), s, $"###{s}")
        End Function

        Public Overridable Iterator Function FindAllStructs(root As SystemLibrary) As IEnumerable(Of RkStruct)

            For Each s In root.AllNamespace.Map(Function(x) x.Structs.Values.Flatten).Flatten.By(Of RkStruct)

                Yield s

                If TypeOf s Is IScope Then

                    For Each s2 In Me.FindInnerStructs(CType(s, IScope))

                        Yield s2
                    Next
                End If
            Next

            For Each f In Me.FindAllMethods(root)

                If TypeOf f Is IScope Then

                    For Each s2 In Me.FindInnerStructs(CType(f, IScope))

                        Yield s2
                    Next
                End If
            Next
        End Function

        Public Overridable Iterator Function FindInnerStructs(scope As IScope) As IEnumerable(Of RkStruct)

            For Each s In scope.Structs.Values.Flatten.By(Of RkStruct)

                Yield s

                If TypeOf s Is IScope Then

                    For Each s2 In Me.FindInnerStructs(CType(s, IScope))

                        Yield s2
                    Next
                End If
            Next
        End Function

        Public Shared Function CreateManglingName(struct As RkStruct) As String

            Return $"{CurrentNamespace(struct.Scope).Name}.{struct.CreateManglingName}"
        End Function

        Public Shared Function CreateManglingName(f As IFunction) As String

            Return $"{CurrentNamespace(f.Scope).Name}.{f.CreateManglingName}"
        End Function

        Public Overridable Function DeclareStructs(root As SystemLibrary) As Dictionary(Of RkStruct, TypeData)

            Dim map As New Dictionary(Of RkStruct, TypeData)
            For Each struct In Me.FindAllStructs(root).Where(Function(x) (Not x.HasGeneric AndAlso Not x.HasIndefinite AndAlso x.StructNode IsNot Nothing AndAlso TypeOf x IsNot RkCILStruct) OrElse x.ClosureEnvironment)

                If map.ContainsKey(struct) Then Continue For
                Dim name = CreateManglingName(struct)
                Dim exists = map.Values.FindFirstOrNull(Function(t) name.Equals(t.Type.FullName))
                map(struct) = If(exists, New TypeData With {.Type = Me.Module.DefineType(name)})
            Next

            root.TupleCache.Values.SortToList.Unique.Each(Sub(x) map.Add(x, New TypeData With {.Type = Me.Module.DefineType($"##Tuple.{x.CreateManglingName}")}))

            For Each v In map

                If v.Value.Constructor IsNot Nothing Then Continue For

                Dim builder = CType(v.Value.Type, TypeBuilder)
                v.Value.Constructor = If(v.Key.Initializer Is Nothing, builder.DefineDefaultConstructor(MethodAttributes.Public), builder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes))
                For Each x In v.Key.Local

                    v.Value.Fields(x.Key) = builder.DefineField(x.Key, Me.RkToCILType(x.Value, map).Type, FieldAttributes.Public)
                Next
            Next
            map.Add(root.NullType, New TypeData With {.Type = GetType(Object)})

            Return map
        End Function

        Public Overridable Iterator Function FindAllMethods(root As SystemLibrary) As IEnumerable(Of IFunction)

            For Each f In root.AllNamespace.Map(Function(x) x.Functions.Values.Flatten).Flatten

                Yield f

                If TypeOf f Is IScope Then

                    For Each f2 In Me.FindInnerMethods(CType(f, IScope))

                        Yield f2
                    Next
                End If
            Next
        End Function

        Public Overridable Iterator Function FindInnerMethods(scope As IScope) As IEnumerable(Of IFunction)

            For Each f In scope.Functions.Values.Flatten

                Yield f

                If TypeOf f Is IScope Then

                    For Each f2 In Me.FindInnerMethods(CType(f, IScope))

                        Yield f2
                    Next
                End If
            Next

            For Each inner In scope.InnerScopes

                For Each f In Me.FindInnerMethods(inner)

                    Yield f
                Next
            Next
        End Function

        Public Overridable Function DeclareMethods(root As SystemLibrary, structs As Dictionary(Of RkStruct, TypeData)) As Dictionary(Of IFunction, MethodInfo)

            Dim map As New Dictionary(Of IFunction, MethodInfo)
            For Each f In Me.FindAllMethods(root).Where(Function(x) Not x.HasGeneric AndAlso Not x.HasIndefinite AndAlso (x.FunctionNode IsNot Nothing OrElse x.Body.Count > 0))

                If map.ContainsKey(f) Then Continue For
                Dim name = CreateManglingName(f)
                Dim exists = map.Values.FindFirstOrNull(Function(t) name.Equals(t.Name))
                map(f) = If(exists, Me.Module.DefineGlobalMethod($"{CurrentNamespace(f.Scope).Name}.{f.CreateManglingName}", MethodAttributes.Static Or MethodAttributes.Public, Me.RkToCILType(f.Return, structs, True).Type, Me.RkToCILType(f.Arguments, structs)))
            Next

            Return map
        End Function

        Public Overridable Sub Emit_WriteLine(il As ILGenerator, Optional t As Type = Nothing)

            If t Is Nothing Then t = GetType(IntPtr)
            il.EmitCall(OpCodes.Call, GetType(System.Console).GetMethod("WriteLine", {t}), Nothing)
        End Sub

        Public Overridable ReadOnly Property Binders As New Dictionary(Of RkFunction, TypeData)

        Public Overridable Function DeclareBind(f As RkFunction, structs As Dictionary(Of RkStruct, TypeData)) As TypeData

            For Each bind In Me.Binders

                If bind.Key.CreateManglingName.Equals(f.CreateManglingName) Then Return bind.Value
            Next

            If Not Me.Binders.ContainsKey(f) Then

                Dim t = Me.Module.DefineType($"Bind_{f.Name}")
                Dim binds = f.Arguments.Where(Function(x) TypeOf x.Value Is RkStruct AndAlso CType(x.Value, RkStruct).ClosureEnvironment)
                Dim args = f.Arguments.Where(Function(x) Not (TypeOf x.Value Is RkStruct AndAlso CType(x.Value, RkStruct).ClosureEnvironment))

                Dim ctor = t.DefineDefaultConstructor(MethodAttributes.Public)
                Dim func = t.DefineField("f", GetType(IntPtr), FieldAttributes.Public)
                Dim fields = binds.Map(Function(x, i) t.DefineField(x.Name, Me.RkToCILType(x.Value, structs).Type, FieldAttributes.Public)).ToArray
                Dim method = t.DefineMethod("Invoke", MethodAttributes.Public, Me.RkToCILType(f.Return, structs, True).Type, Me.RkToCILType(args.ToList, structs))

                Dim il = method.GetILGenerator
                fields.Each(
                    Sub(x)
                        il.Emit(OpCodes.Ldarg_0)
                        il.Emit(OpCodes.Ldfld, x)
                    End Sub)
                args.Each(Sub(x, i) il.Emit(OpCodes.Ldarg, i + 1))
                il.Emit(OpCodes.Ldarg_0)
                il.Emit(OpCodes.Ldfld, func)
                il.EmitCalli(OpCodes.Calli, CallingConventions.Standard, method.ReturnType, Me.RkToCILType(f.Arguments.ToList, structs), Nothing)
                il.Emit(OpCodes.Ret)

                Dim builder = New TypeData With {.Type = t, .Constructor = ctor}
                builder.Fields(func.Name) = func
                fields.Each(Sub(x) builder.Fields(x.Name) = x)
                builder.Methods(method.Name) = method

                Me.Binders.Add(f, builder)
            End If

            Return Me.Binders(f)
        End Function

        Public Overridable Sub DeclareStatements(root As SystemLibrary, functions As Dictionary(Of IFunction, MethodInfo), structs As Dictionary(Of RkStruct, TypeData))

            Dim gen_il_loadc =
                Sub(il As ILGenerator, v As OpValue)

                    If TypeOf v Is OpNumeric32 Then

                        Dim num = CType(v, OpNumeric32)
                        Select Case num.Numeric

                            'Case -1 : il.Emit(OpCodes.Ldc_I4_M1)
                            Case 0 : il.Emit(OpCodes.Ldc_I4_0)
                            Case 1 : il.Emit(OpCodes.Ldc_I4_1)
                            Case 2 : il.Emit(OpCodes.Ldc_I4_2)
                            Case 3 : il.Emit(OpCodes.Ldc_I4_3)
                            Case 4 : il.Emit(OpCodes.Ldc_I4_4)
                            Case 5 : il.Emit(OpCodes.Ldc_I4_5)
                            Case 6 : il.Emit(OpCodes.Ldc_I4_6)
                            Case 7 : il.Emit(OpCodes.Ldc_I4_7)
                            Case 8 : il.Emit(OpCodes.Ldc_I4_8)

                            Case Else
                                il.Emit(OpCodes.Ldc_I4, num.Numeric)

                        End Select

                    ElseIf TypeOf v Is OpString Then

                        Dim str = CType(v, OpString)
                        il.Emit(OpCodes.Ldstr, str.String)

                    ElseIf TypeOf v Is OpNull Then

                        il.Emit(OpCodes.Ldnull)

                    Else

                        Debug.Fail("miss load value")
                    End If
                End Sub

            Dim gen_il_load =
                Sub(il As ILGenerator, index As Integer, ref As Boolean)

                    If ref Then

                        If index >= 0 Then

                            il.Emit(OpCodes.Ldloca, index)
                        Else

                            il.Emit(OpCodes.Ldarga, -(index + 1))
                        End If
                    Else

                        Select Case index

                            Case 0 : il.Emit(OpCodes.Ldloc_0)
                            Case 1 : il.Emit(OpCodes.Ldloc_1)
                            Case 2 : il.Emit(OpCodes.Ldloc_2)
                            Case 3 : il.Emit(OpCodes.Ldloc_3)

                            Case -1 : il.Emit(OpCodes.Ldarg_0)
                            Case -2 : il.Emit(OpCodes.Ldarg_1)
                            Case -3 : il.Emit(OpCodes.Ldarg_2)
                            Case -4 : il.Emit(OpCodes.Ldarg_3)

                            Case Else

                                If index >= 0 Then

                                    il.Emit(OpCodes.Ldloc, index)
                                Else

                                    il.Emit(OpCodes.Ldarg, -(index + 1))
                                End If
                        End Select
                    End If
                End Sub

            Dim gen_il_store =
                Sub(il As ILGenerator, index As Integer)

                    Select Case index

                        Case 0 : il.Emit(OpCodes.Stloc_0)
                        Case 1 : il.Emit(OpCodes.Stloc_1)
                        Case 2 : il.Emit(OpCodes.Stloc_2)
                        Case 3 : il.Emit(OpCodes.Stloc_3)
                        Case Else

                            If index >= 0 Then

                                il.Emit(OpCodes.Stloc, index)
                            Else

                                il.Emit(OpCodes.Starg, -(index + 1))
                            End If
                    End Select
                End Sub

            Dim gen_local_store =
                Function(args As List(Of NamedValue))

                    Dim locals = args.Map(Function(x) x.Name).ToHash_ValueDerivation(Function(x, i) -i - 1)
                    Dim max_local = 0
                    Return Function(il As ILGenerator, v As OpValue)

                               Dim name = v.Name
                               If locals.ContainsKey(name) Then Return locals(name)
                               il.DeclareLocal(Me.RkToCILType(v.Type, structs).Type).SetLocalSymInfo(name)
                               locals(name) = max_local
                               max_local += 1
                               Return max_local - 1
                           End Function
                End Function

            For Each f In functions.Where(Function(x) TypeOf x.Value Is MethodBuilder)

                Dim get_local = gen_local_store(f.Key.Arguments)
                Me.DeclareStatement(
                    root,
                    f.Key,
                    CType(f.Value, MethodBuilder).GetILGenerator,
                    Sub(il, v, ref)

                        If TypeOf v.Type Is RkFunction AndAlso Not CType(v.Type, RkFunction).IsAnonymous Then

                            Dim fn = CType(v.Type, RkFunction)
                            Dim bind = Me.DeclareBind(fn, structs)
                            Dim r = New OpValue With {.Name = v.Type.Name, .Type = v.Type}
                            il.Emit(OpCodes.Newobj, bind.Constructor)
                            Dim index = get_local(il, r)
                            gen_il_store(il, index)
                            bind.Fields.Each(
                                Sub(x)

                                    gen_il_load(il, index, ref)
                                    If x.Key.Equals("f") Then

                                        il.Emit(OpCodes.Ldftn, Me.RkToCILFunction(fn, functions, structs))
                                    Else

                                        gen_il_load(il, get_local(il, New OpValue With {.Name = x.Key, .Type = fn.Arguments.FindFirst(Function(arg) arg.Name.Equals(x.Key)).Value}), ref)
                                    End If
                                    il.Emit(OpCodes.Stfld, x.Value)
                                End Sub)
                            gen_il_load(il, index, ref)
                            il.Emit(OpCodes.Ldftn, bind.GetMethod("Invoke"))
                            il.Emit(OpCodes.Newobj, Me.RkFunctionToCILType(fn, structs).Constructor)

                        ElseIf TypeOf v Is OpNumeric32 OrElse
                            TypeOf v Is OpString OrElse
                            TypeOf v Is OpNull Then

                            gen_il_loadc(il, v)

                        ElseIf TypeOf v Is OpProperty Then

                            Dim prop = CType(v, OpProperty)
                            gen_il_load(il, get_local(il, prop.Receiver), ref)
                            il.Emit(OpCodes.Ldfld, Me.RkToCILType(CType(prop.Receiver.Type, RkStruct), structs).GetField(prop.Name))

                        ElseIf TypeOf v Is OpValue Then

                            gen_il_load(il, get_local(il, v), ref)
                        End If
                    End Sub,
                    Sub(il, v)

                        If TypeOf v Is OpProperty Then

                            Dim prop = CType(v, OpProperty)
                            il.Emit(OpCodes.Stfld, structs(CType(prop.Receiver.Type, RkStruct)).Fields(prop.Name))
                        Else
                            gen_il_store(il, get_local(il, v))
                        End If
                    End Sub,
                    f.Key.Body,
                    functions,
                    structs
                )
            Next

            For Each s In structs.Where(Function(x) x.Key.Initializer IsNot Nothing)

                Dim get_local = gen_local_store(New List(Of NamedValue))
                Me.DeclareStatement(
                    root,
                    s.Key.Initializer,
                    CType(s.Value.Constructor, ConstructorBuilder).GetILGenerator,
                    Sub(il, v, ref)

                        If TypeOf v.Type Is RkFunction AndAlso Not CType(v.Type, RkFunction).IsAnonymous Then

                            Debug.Fail("not yet")

                        ElseIf TypeOf v Is OpNumeric32 OrElse
                            TypeOf v Is OpString OrElse
                            TypeOf v Is OpNull Then

                            gen_il_loadc(il, v)

                        ElseIf TypeOf v Is OpProperty Then

                            Dim prop = CType(v, OpProperty)
                            gen_il_load(il, get_local(il, prop.Receiver), ref)
                            il.Emit(OpCodes.Ldfld, Me.RkToCILType(CType(prop.Receiver.Type, RkStruct), structs).GetField(prop.Name))

                        ElseIf TypeOf v Is OpValue Then

                            gen_il_load(il, get_local(il, v), ref)
                        End If
                    End Sub,
                    Sub(il, v)

                        Dim index = get_local(il, v)
                        gen_il_store(il, index)
                        il.Emit(OpCodes.Ldarg_0)
                        gen_il_load(il, index, False)
                        il.Emit(OpCodes.Stfld, s.Value.GetField(v.Name))
                    End Sub,
                    s.Key.Initializer.Body,
                    functions,
                    structs
                )
            Next
        End Sub

        Public Overridable Sub DeclareStatement(
                root As SystemLibrary,
                fn As IFunction,
                il As ILGenerator,
                gen_il_load As Action(Of ILGenerator, OpValue, Boolean),
                gen_il_store As Action(Of ILGenerator, OpValue),
                stmts As List(Of InCode0),
                functions As Dictionary(Of IFunction, MethodInfo),
                structs As Dictionary(Of RkStruct, TypeData)
            )

            Dim gen_il_3op =
                Sub(ope As OpCode, code As InCode)

                    gen_il_load(il, code.Left, False)
                    gen_il_load(il, code.Right, False)
                    il.Emit(ope)
                    If code.Return IsNot Nothing Then gen_il_store(il, code.Return)
                End Sub

            Dim gen_il_3op_not =
                Sub(ope As OpCode, code As InCode)

                    gen_il_load(il, code.Left, False)
                    gen_il_load(il, code.Right, False)
                    il.Emit(ope)
                    il.Emit(OpCodes.Ldc_I4_0)
                    il.Emit(OpCodes.Ceq)
                    If code.Return IsNot Nothing Then gen_il_store(il, code.Return)
                End Sub

            Dim gen_il_3ad =
                Sub(ope As OpCode, code As InCode)

                    If code.Left.Type.Name.Equals("String") Then

                        gen_il_load(il, code.Left, False)
                        gen_il_load(il, code.Right, False)
                        il.EmitCall(OpCodes.Call, GetType(System.String).GetMethod("Concat", {GetType(String), GetType(String)}), {GetType(String), GetType(String)})
                        If code.Return IsNot Nothing Then gen_il_store(il, code.Return)
                    Else
                        gen_il_3op(ope, code)
                    End If
                End Sub

            Dim gen_il_uminus =
                Sub(ope As OpCode, code As InCode)

                    gen_il_load(il, New OpNumeric32 With {.Numeric = 0}, False)
                    gen_il_load(il, code.Left, False)
                    il.Emit(ope)
                    If code.Return IsNot Nothing Then gen_il_store(il, code.Return)
                End Sub

            Dim get_ctor =
                Function(r As RkStruct)

                    Return Me.RkToCILType(r, structs).Constructor
                End Function

            Dim labels = stmts.Where(Function(x) TypeOf x Is InLabel).ToHash_ValueDerivation(Function(x) il.DefineLabel)

            For Each stmt In stmts

                If TypeOf stmt Is IReturnBind Then

                    Dim bind = CType(stmt, IReturnBind)
                    If TypeOf bind.Return?.Type Is RkFunction AndAlso bind.Return.Type.HasGeneric Then Continue For

                    If TypeOf bind.Return Is OpProperty Then

                        Dim prop = CType(bind.Return, OpProperty)
                        If prop.Receiver IsNot Nothing Then gen_il_load(il, prop.Receiver, False)
                    End If
                End If

                Select Case stmt.Operator
                    Case InOperator.Plus : gen_il_3ad(OpCodes.Add, CType(stmt, InCode))
                    Case InOperator.Minus : gen_il_3op(OpCodes.Sub, CType(stmt, InCode))
                    Case InOperator.Mul : gen_il_3op(OpCodes.Mul, CType(stmt, InCode))
                    Case InOperator.Div : gen_il_3op(OpCodes.Div, CType(stmt, InCode))
                    Case InOperator.Equal : gen_il_3op(OpCodes.Ceq, CType(stmt, InCode))
                    Case InOperator.Lt : gen_il_3op(OpCodes.Clt, CType(stmt, InCode))
                    Case InOperator.Lte : gen_il_3op_not(OpCodes.Cgt, CType(stmt, InCode))
                    Case InOperator.Gt : gen_il_3op(OpCodes.Cgt, CType(stmt, InCode))
                    Case InOperator.Gte : gen_il_3op_not(OpCodes.Clt, CType(stmt, InCode))
                    Case InOperator.UMinus : gen_il_uminus(OpCodes.Sub, CType(stmt, InCode))

                    Case InOperator.Bind
                        Dim bind = CType(stmt, InCode)
                        If bind.Left IsNot Nothing Then

                            gen_il_load(il, bind.Left, False)
                            gen_il_store(il, bind.Return)
                        End If

                    Case InOperator.Dot
                        Dim dot = CType(stmt, InCode)
                        If dot.Return?.Type Is Nothing Then

                            ' nothing

                        ElseIf dot.Left?.Type Is Nothing Then

                            ' nothing

                        ElseIf TypeOf dot.Left.Type Is RkNamespace AndAlso dot.Right.Type Is Nothing Then

                            ' nothing

                        ElseIf TypeOf dot.Left.Type Is RkNamespace AndAlso TypeOf dot.Right.Type Is RkStruct Then

                            ' nothing
                        Else

                            gen_il_load(il, dot.Left, False)
                            il.Emit(OpCodes.Ldfld, Me.RkToCILType(dot.Left.Type, structs).GetField(dot.Right.Name))
                            gen_il_store(il, dot.Return)
                        End If

                    Case InOperator.Call
                        If TypeOf stmt Is InLambdaCall Then

                            Dim cc = CType(stmt, InLambdaCall)
                            Dim bind = Me.RkFunctionToCILType(cc.Function, structs)
                            gen_il_load(il, cc.Value, False)
                            cc.Arguments.Each(Sub(arg) gen_il_load(il, arg, False))
                            il.EmitCall(OpCodes.Callvirt, bind.GetMethod("Invoke"), Nothing)
                            If cc.Return IsNot Nothing Then gen_il_store(il, cc.Return)
                        Else

                            Dim cc = CType(stmt, InCall)
                            Dim env = 0
                            cc.Function.Arguments.Each(
                                Sub(x)

                                    If TypeOf x.Value Is RkStruct AndAlso CType(x.Value, RkStruct).ClosureEnvironment Then

                                        gen_il_load(il, New OpValue With {.Name = x.Name, .Type = x.Value}, False)
                                        env += 1
                                    End If
                                End Sub)
                            cc.Arguments.Each(
                                Sub(arg, i)

                                    Dim ref = False
                                    If TypeOf cc.Function Is RkCILFunction Then

                                        Dim f = CType(cc.Function, RkCILFunction)
                                        If f.MethodInfo.IsStatic OrElse i > 0 Then

                                            Dim p = f.MethodInfo.GetParameters(If(Not f.MethodInfo.IsStatic, i - 1, i))
                                            If p.IsOut Then ref = True
                                        Else

                                            ref = f.MethodInfo.DeclaringType.IsValueType
                                        End If
                                    End If
                                    gen_il_load(il, arg, ref)
                                    If Not ref Then

                                        Dim ai = cc.Function.Arguments(env + i).Value
                                        If Me.RkToCILType(arg.Type, structs).Type.IsValueType AndAlso
                                            Not Me.RkToCILType(ai, structs).Type.IsValueType Then

                                            il.Emit(OpCodes.Box, Me.RkToCILType(arg.Type, structs).Type)
                                        End If
                                    End If
                                End Sub)
                            If TypeOf cc.Function Is RkCILConstructor Then

                                Dim f = CType(cc.Function, RkCILConstructor)
                                If f.ConstructorInfo Is Nothing Then

                                    Debug.Assert(cc.Arguments.Count = 0, "invalid arguments")
                                    Debug.Assert(cc.Return IsNot Nothing, "invalid return")
                                    Debug.Fail("not yet")
                                    'il.Emit(OpCodes.Ldloca, ) <- cc.Return
                                    il.Emit(OpCodes.Initobj, f.TypeInfo)
                                Else
                                    il.Emit(OpCodes.Newobj, Me.RkToCILConstructor(f, structs))
                                End If
                            Else
                                il.Emit(OpCodes.Call, Me.RkToCILFunction(cc.Function, functions, structs))
                            End If

                            If cc.Return Is Nothing Then

                                If cc.Function.Return IsNot Nothing Then il.Emit(OpCodes.Pop)
                            Else
                                gen_il_store(il, cc.Return)
                            End If
                        End If

                    Case InOperator.Return
                        If TypeOf stmt Is InCode Then gen_il_load(il, CType(stmt, InCode).Left, False)
                        il.Emit(OpCodes.Ret)

                    Case InOperator.Alloc
                        Dim alloc = CType(stmt, InCode)
                        il.Emit(OpCodes.Newobj, get_ctor(CType(alloc.Left.Type, RkStruct)))
                        gen_il_store(il, alloc.Return)

                    Case InOperator.Array
                        Dim alloc = CType(stmt, InCode)
                        Dim array = CType(alloc.Left, OpArray)
                        Dim array_type = Me.RkToCILType(array.Type, structs)
                        If array.List.Count >= 3 Then

                            Dim t = Me.RkToCILType(array.List(0).Type, structs)
                            If t.Type IsNot GetType(Integer) Then GoTo ARRAY_CREATE_

                            ' $0 = newarr[size]
                            il.Emit(OpCodes.Ldc_I4, array.List.Count)
                            il.Emit(OpCodes.Newarr, t.Type)

                            ' $1 = ldtoken bytearray(...)
                            ' $2 = InitializeArray($0, $1)
                            il.Emit(OpCodes.Dup)
                            il.Emit(OpCodes.Ldtoken, Me.Module.DefineInitializedData("Array", array.List.Map(Function(x) BitConverter.GetBytes(CType(x, OpNumeric32).Numeric)).Flatten.ToArray, FieldAttributes.InitOnly))
                            il.EmitCall(OpCodes.Call, GetType(System.Runtime.CompilerServices.RuntimeHelpers).GetMethod("InitializeArray"), Nothing)

                            ' ret = List.of(Int)($2)
                            il.Emit(OpCodes.Newobj, array_type.Type.GetConstructor({GetType(IEnumerable(Of Integer))}))
                            gen_il_store(il, alloc.Return)
                        Else

ARRAY_CREATE_:
                            il.Emit(OpCodes.Newobj, get_ctor(CType(array.Type, RkStruct)))
                            For Each x In array.List

                                il.Emit(OpCodes.Dup)
                                gen_il_load(il, x, False)
                                il.Emit(OpCodes.Call, array_type.GetMethod("Add"))
                            Next
                            gen_il_store(il, alloc.Return)
                        End If

                    Case InOperator.If
                        Dim if_ = CType(stmt, InIf)
                        gen_il_load(il, if_.Condition, False)
                        il.Emit(OpCodes.Brfalse, labels(if_.Else))

                    Case InOperator.Goto
                        il.Emit(OpCodes.Br, labels(CType(stmt, InGoto).Label))

                    Case InOperator.Label
                        il.MarkLabel(labels(stmt))

                    Case InOperator.Cast
                        Dim cast = CType(stmt, InCode)
                        gen_il_load(il, cast.Left, False)
                        If TypeOf cast.Right.Type Is RkCILStruct Then

                            Dim ct = CType(cast.Right.Type, RkCILStruct)
                            If Me.RkToCILType(cast.Left.Type, structs).Type.IsValueType Then

                                Select Case True
                                    Case ct.TypeInfo Is GetType(Int32) : il.Emit(OpCodes.Conv_I4)
                                    Case ct.TypeInfo Is GetType(Int64) : il.Emit(OpCodes.Conv_I8)
                                    Case ct.TypeInfo Is GetType(Int16) : il.Emit(OpCodes.Conv_I2)
                                    Case ct.TypeInfo Is GetType(Byte) : il.Emit(OpCodes.Conv_U1)

                                    Case Else
                                        GoTo CLASS_CAST_
                                End Select
                            Else

                                il.Emit(OpCodes.Unbox_Any, ct.TypeInfo)
                            End If
                        Else
CLASS_CAST_:
                            il.Emit(OpCodes.Isinst, Me.RkToCILType(cast.Right.Type, structs).Type)
                        End If
                        gen_il_store(il, cast.Return)

                    Case InOperator.CanCast
                        Dim cancast = CType(stmt, InCode)
                        Dim t = Me.RkToCILType(cancast.Right.Type, structs).Type
                        gen_il_load(il, cancast.Left, False)
                        If TypeOf cancast.Left.Type Is RkCILStruct AndAlso CType(cancast.Left.Type, RkCILStruct).TypeInfo.IsValueType Then

                            il.Emit(OpCodes.Box, t)
                        End If
                        If cancast.Right.Type Is root.NullType Then

                            il.Emit(OpCodes.Ldnull)
                            il.Emit(OpCodes.Ceq)
                        Else

                            il.Emit(OpCodes.Isinst, t)
                            il.Emit(OpCodes.Ldnull)
                            il.Emit(OpCodes.Cgt_Un)
                        End If
                        gen_il_store(il, cancast.Return)

                    Case Else
                        Debug.Fail("not yet")
                End Select
            Next

            If stmts.Count = 0 OrElse stmts(stmts.Count - 1).Operator <> InOperator.Return Then

                If fn.Return IsNot Nothing AndAlso
                    Not (TypeOf fn Is RkNativeFunction AndAlso CType(fn, RkNativeFunction).Operator = InOperator.Alloc) Then il.Emit(OpCodes.Ldnull)

                il.Emit(OpCodes.Ret)
            End If
        End Sub

        Public Overridable Function RkToCILType(r As IType, structs As Dictionary(Of RkStruct, TypeData), Optional allow_void As Boolean = False) As TypeData

            If r Is Nothing Then

                If Not allow_void Then Throw New ArgumentNullException(NameOf(r))
                Return New TypeData With {.Type = GetType(System.Void), .Constructor = Nothing}
            End If

            If TypeOf r Is RkFunction Then Return Me.RkFunctionToCILType(CType(r, RkFunction), structs)
            If TypeOf r Is RkCILStruct Then

                Dim s = CType(r, RkCILStruct)
                If s.Apply.Count > 0 Then

                    Dim a = s.TypeInfo.MakeGenericType(s.Apply.Map(Function(x) Me.RkToCILType(x, structs).Type).ToArray)
                    Return New TypeData With {.Type = a.GetTypeInfo, .Constructor = a.GetConstructor(New Type() {})}
                Else
                    Return New TypeData With {.Type = s.TypeInfo, .Constructor = s.TypeInfo.GetConstructor(New Type() {})}
                End If
            End If
            If TypeOf r Is RkGenericEntry Then Return New TypeData With {.Type = GetType(System.Object), .Constructor = Nothing}
            If TypeOf r IsNot RkStruct Then Throw New ArgumentException("invalid RkStruct", NameOf(r))
            Return structs(CType(r, RkStruct))
        End Function

        Public Overridable Function RkFunctionToCILType(r As RkFunction, structs As Dictionary(Of RkStruct, TypeData)) As TypeData

            Dim args = r.Arguments.Where(Function(x) Not (TypeOf x.Value Is RkStruct AndAlso CType(x.Value, RkStruct).ClosureEnvironment)).Map(Function(x) Me.RkToCILType(x.Value, structs).Type).ToList
            If r.Return Is Nothing Then

                If args.Count = 0 Then

                    Dim t = Type.GetType("System.Action")
                    Return New TypeData With {.Type = t, .Constructor = t.GetConstructor({GetType(Object), GetType(IntPtr)})}
                Else

                    Dim t = Type.GetType($"System.Action`{args.Count}")
                    Dim g = t.MakeGenericType(args.ToArray)
                    Return New TypeData With {.Type = g, .Constructor = g.GetConstructor({GetType(Object), GetType(IntPtr)})}
                End If
            Else

                Dim t = Type.GetType($"System.Func`{args.Count + 1}")
                args.Add(Me.RkToCILType(r.Return, structs).Type)
                Dim g = t.MakeGenericType(args.ToArray)
                Return New TypeData With {.Type = g, .Constructor = g.GetConstructor({GetType(Object), GetType(IntPtr)})}
            End If
        End Function

        Public Overridable Function RkToCILType(r As List(Of NamedValue), structs As Dictionary(Of RkStruct, TypeData)) As System.Type()

            Return r.Map(Function(x) Me.RkToCILType(x.Value, structs).Type).ToArray
        End Function

        Public Overridable Function RkToCILType(r As List(Of OpValue), structs As Dictionary(Of RkStruct, TypeData)) As System.Type()

            Return r.Map(Function(x) Me.RkToCILType(x.Type, structs).Type).ToArray
        End Function

        Public Overridable Function RkToCILFunction(f As RkFunction, functions As Dictionary(Of IFunction, MethodInfo), structs As Dictionary(Of RkStruct, TypeData)) As MethodInfo

            If TypeOf f Is RkCILFunction Then

                If f.Apply.Count > 0 Then

                    Dim method = CType(f, RkCILFunction).MethodInfo
                    Dim t As Type
                    If method.IsStatic Then

                        t = method.DeclaringType
                        t = t.MakeGenericType(t.GetGenericArguments.Map(Function(x) Me.RkToCILType(f.Apply(f.GenericBase.Generics.FindFirst(Function(g) g.Name.Equals(x.Name)).ApplyIndex), structs).Type).ToArray)
                    Else

                        t = Me.RkToCILType(f.Arguments(0).Value, structs).Type
                    End If

                    Dim m = t.GetMethods.FindFirst(Function(x) x.Name.Equals(f.Name))
                    If Not m.IsGenericMethod Then Return m
                    Return m.MakeGenericMethod(m.GetGenericArguments.Map(Function(x) Me.RkToCILType(f.Apply(f.GenericBase.Generics.FindFirst(Function(g) g.Name.Equals(x.Name)).ApplyIndex), structs).Type).ToArray)
                Else
                    Return CType(f, RkCILFunction).MethodInfo
                End If
            End If

            If functions.ContainsKey(f) Then Return functions(f)

            Dim namematch = functions.FindFirstOrNull(Function(kv) kv.Key.Name.Equals(f.Name))
            If namematch.Value IsNot Nothing Then Return namematch.Value

            Throw New MissingMethodException
        End Function

        Public Overridable Function RkToCILConstructor(f As RkCILConstructor, structs As Dictionary(Of RkStruct, TypeData)) As ConstructorInfo

            If f.ConstructorInfo.DeclaringType.IsGenericType Then

                ' ToDo: parameter match
                Dim r = Me.RkToCILType(f.Return, structs)
                f.ConstructorInfo = r.Type.GetConstructors.FindFirst(Function(x) x.GetParameters.Length = f.ConstructorInfo.GetParameters.Length)
            End If
            Return f.ConstructorInfo
        End Function


    End Class

End Namespace
