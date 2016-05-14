Imports System
Imports System.Diagnostics
Imports System.Collections.Generic
Imports System.Reflection
Imports System.Reflection.Emit
Imports Roku.Manager
Imports Roku.Util.ArrayExtension


Namespace Architecture

    Public Class CommonIL

#Region "libs"

        Public Class Export

            Public Overridable Property Name As String
            Public Overridable Property Assembly As String
            Public Overridable Property [Class] As String
            Public Overridable Property Method As String
            Public Overridable Property Arguments As Type()
        End Class

        Public Overridable ReadOnly Property Exports As Export() = New Export() {
                New Export With {.Name = "print", .Assembly = "mscorlib", .Class = "System.Console", .Method = "WriteLine", .Arguments = New Type() {GetType(String)}},
                New Export With {.Name = "print", .Assembly = "mscorlib", .Class = "System.Console", .Method = "WriteLine", .Arguments = New Type() {GetType(Integer)}}
            }

        Public Class TypeData

            Public Overridable Property Type As System.Type
            Public Overridable Property Constructor As ConstructorInfo
            Public Overridable Property Fields As New Dictionary(Of String, FieldInfo)
            Public Overridable Property Methods As New Dictionary(Of String, MethodInfo)

            Public Overridable Function GetField(name As String) As FieldInfo

                Return If(Me.Fields.ContainsKey(name), Me.Fields(name), Me.Type.GetField(name))
            End Function

            Public Overridable Function GetMethod(name As String) As MethodInfo

                Return If(Me.Methods.ContainsKey(name), Me.Methods(name), Me.Type.GetMethod(name))
            End Function
        End Class

#End Region

        Public Overridable Property [Module] As ModuleBuilder

        Public Overridable Sub Assemble(ns As SystemLirary, entrypoint As RkNamespace, path As String, subsystem As PEFileKinds)

            Dim name As New AssemblyName(entrypoint.Name)
            Dim asm = System.AppDomain.CurrentDomain.DefineDynamicAssembly(name, AssemblyBuilderAccess.Save)
            Me.Module = asm.DefineDynamicModule(entrypoint.Name, System.IO.Path.GetRandomFileName, True)

            Dim structs = Me.DeclareStructs(ns)
            Dim functions = Me.DeclareMethods(ns, structs)
            Me.DeclareStatements(functions, structs)
            structs.Do(Sub(x) If TypeOf x.Value.Type Is TypeBuilder Then CType(x.Value.Type, TypeBuilder).CreateType())
            Me.Binders.Do(Sub(x) CType(x.Value.Type, TypeBuilder).CreateType())

            If subsystem <> PEFileKinds.Dll Then

                ' global sub main() {entrypoint.###.ctor();}
                Dim ctor = entrypoint.LoadFunction(".ctor")
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

        Public Overridable Function DeclareStructs(root As SystemLirary) As Dictionary(Of RkStruct, TypeData)

            Dim map As New Dictionary(Of RkStruct, TypeData)
            'map(root.LoadStruct("Char")) = New TypeData With {.Type = GetType(Char), .Constructor = GetType(Char).GetConstructor(Type.EmptyTypes)}
            'map(root.LoadStruct("Int16")) = New TypeData With {.Type = GetType(Int16), .Constructor = GetType(Int16).GetConstructor(Type.EmptyTypes)}
            'map(root.LoadStruct("Int32")) = New TypeData With {.Type = GetType(Int32), .Constructor = GetType(Int32).GetConstructor(Type.EmptyTypes)}
            'map(root.LoadStruct("Int64")) = New TypeData With {.Type = GetType(Int64), .Constructor = GetType(Int64).GetConstructor(Type.EmptyTypes)}
            'map(root.LoadStruct("String")) = New TypeData With {.Type = GetType(String), .Constructor = GetType(String).GetConstructor(Type.EmptyTypes)}
            For Each ns In root.AllNamespace

                For Each ss In ns.Structs

                    For Each struct In ss.Value.Where(Function(x) (Not x.HasGeneric AndAlso x.StructNode IsNot Nothing AndAlso TypeOf x IsNot RkCILStruct) OrElse x.ClosureEnvironment)

                        map(struct) = New TypeData With {.Type = Me.Module.DefineType($"{struct.Namespace.Name}.{struct.CreateManglingName}")}
                    Next
                Next
            Next

            For Each v In map.Where(Function(x) TypeOf x.Value.Type Is TypeBuilder)

                Dim builder = CType(v.Value.Type, TypeBuilder)
                v.Value.Constructor = If(v.Key.Initializer Is Nothing, builder.DefineDefaultConstructor(MethodAttributes.Public), builder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes))
                For Each x In v.Key.Local

                    v.Value.Fields(x.Key) = builder.DefineField(x.Key, Me.RkToCILType(x.Value, map).Type, FieldAttributes.Public)
                Next
            Next

            For Each p In root.Structs("Array").Where(Function(x) Not x.HasGeneric)

                Dim t = Me.RkArrayToCILArray(p, map)
                map(p) = New TypeData With {.Type = t, .Constructor = t.GetConstructor(Type.EmptyTypes)}
            Next

            Return map
        End Function

        Public Overridable Function DeclareMethods(root As SystemLirary, structs As Dictionary(Of RkStruct, TypeData)) As Dictionary(Of RkFunction, MethodInfo)

            Dim map As New Dictionary(Of RkFunction, MethodInfo)
            For Each ns In root.AllNamespace

                For Each fs In ns.Functions

                    Dim fxs = fs.Value.Where(Function(x) Not x.HasGeneric AndAlso x.FunctionNode IsNot Nothing).ToArray
                    For Each f In fxs

                        Dim args = Me.RkToCILType(f.Arguments, structs)
                        'Debug.Assert(f.Body.Count > 0, $"{f} statement is nothing")
                        map(f) = Me.Module.DefineGlobalMethod($"{f.Namespace.Name}.{If(fxs.Length = 1, Me.ConvertValidName(f.Name), f.CreateManglingName)}", MethodAttributes.Static Or MethodAttributes.Public, Me.RkToCILType(f.Return, structs).Type, args)
                    Next

                    For Each f In fs.Value.Where(Function(x) Not x.HasGeneric AndAlso x.FunctionNode Is Nothing AndAlso TypeOf x IsNot RkCILFunction)

                        Dim args = Me.RkToCILType(f.Arguments, structs)
                        Dim export = Me.Exports.Where(Function(x) f.Name.Equals(x.Name) AndAlso args.And(Function(arg, i) arg Is x.Arguments(i)))
                        If Not export.IsNull Then

                            Dim e = export.Car
                            map(f) = System.Reflection.Assembly.Load(e.Assembly).GetType(e.Class).GetMethod(e.Method, e.Arguments)
                        End If
                    Next
                Next
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
                Dim method = t.DefineMethod("Invoke", MethodAttributes.Public, Me.RkToCILType(f.Return, structs).Type, Me.RkToCILType(args.ToList, structs))

                Dim il = method.GetILGenerator
                fields.Do(
                    Sub(x)
                        il.Emit(OpCodes.Ldarg_0)
                        il.Emit(OpCodes.Ldfld, x)
                    End Sub)
                args.Do(Sub(x, i) il.Emit(OpCodes.Ldarg, i + 1))
                il.Emit(OpCodes.Ldarg_0)
                il.Emit(OpCodes.Ldfld, func)
                il.EmitCalli(OpCodes.Calli, CallingConventions.Standard, method.ReturnType, Me.RkToCILType(f.Arguments.ToList, structs), Nothing)
                il.Emit(OpCodes.Ret)

                Dim builder = New TypeData With {.Type = t, .Constructor = ctor}
                builder.Fields(func.Name) = func
                fields.Do(Sub(x) builder.Fields(x.Name) = x)
                builder.Methods(method.Name) = method

                Me.Binders.Add(f, builder)
            End If

            Return Me.Binders(f)
        End Function

        Public Overridable Sub DeclareStatements(functions As Dictionary(Of RkFunction, MethodInfo), structs As Dictionary(Of RkStruct, TypeData))

            Dim gen_il_loadc =
                Sub(il As ILGenerator, v As RkValue)

                    If TypeOf v Is RkNumeric32 Then

                        Dim num = CType(v, RkNumeric32)
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

                    ElseIf TypeOf v Is RkString Then

                        Dim str = CType(v, RkString)
                        il.Emit(OpCodes.Ldstr, str.String)

                    Else

                        Debug.Fail("miss load value")
                    End If
                End Sub

            Dim gen_il_load =
                Sub(il As ILGenerator, index As Integer)

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
                    Return Function(il As ILGenerator, v As RkValue)

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
                    CType(f.Value, MethodBuilder).GetILGenerator,
                    Sub(il, v)

                        If TypeOf v.Type Is RkFunction AndAlso Not CType(v.Type, RkFunction).IsAnonymous Then

                            Dim bind = Me.DeclareBind(CType(v.Type, RkFunction), structs)
                            Dim r = New RkValue With {.Name = v.Type.Name, .Type = v.Type}
                            il.Emit(OpCodes.Newobj, bind.Constructor)
                            Dim index = get_local(il, r)
                            gen_il_store(il, index)
                            bind.Fields.Do(
                                Sub(x)

                                    gen_il_load(il, index)
                                    If x.Key.Equals("f") Then

                                        il.Emit(OpCodes.Ldftn, Me.RkToCILFunction(CType(v.Type, RkFunction), functions, structs))
                                    Else

                                        gen_il_load(il, get_local(il, New RkValue With {.Name = x.Key}))
                                    End If
                                    il.Emit(OpCodes.Stfld, x.Value)
                                End Sub)
                            gen_il_load(il, index)
                            il.Emit(OpCodes.Ldftn, bind.GetMethod("Invoke"))
                            il.Emit(OpCodes.Newobj, Me.RkFunctionToCILType(CType(v.Type, RkFunction), structs).Constructor)


                        ElseIf TypeOf v Is RkNumeric32 OrElse
                            TypeOf v Is RkString Then

                            gen_il_loadc(il, v)

                        ElseIf TypeOf v Is RkProperty Then

                            Dim prop = CType(v, RkProperty)
                            gen_il_load(il, get_local(il, prop.Receiver))
                            il.Emit(OpCodes.Ldfld, Me.RkToCILType(CType(prop.Receiver.Type, RkStruct), structs).GetField(prop.Name))

                        ElseIf TypeOf v Is RkValue Then

                            gen_il_load(il, get_local(il, v))
                        End If
                    End Sub,
                    Sub(il, v)

                        If TypeOf v Is RkProperty Then

                            Dim prop = CType(v, RkProperty)
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
                    CType(s.Value.Constructor, ConstructorBuilder).GetILGenerator,
                    Sub(il, v)

                        If TypeOf v.Type Is RkFunction AndAlso Not CType(v.Type, RkFunction).IsAnonymous Then

                            Debug.Fail("not yet")

                        ElseIf TypeOf v Is RkNumeric32 OrElse
                            TypeOf v Is RkString Then

                            gen_il_loadc(il, v)

                        ElseIf TypeOf v Is RkProperty Then

                            Dim prop = CType(v, RkProperty)
                            gen_il_load(il, get_local(il, prop.Receiver))
                            il.Emit(OpCodes.Ldfld, Me.RkToCILType(CType(prop.Receiver.Type, RkStruct), structs).GetField(prop.Name))

                        ElseIf TypeOf v Is RkValue Then

                            gen_il_load(il, get_local(il, v))
                        End If
                    End Sub,
                    Sub(il, v)

                        Dim index = get_local(il, v)
                        gen_il_store(il, index)
                        il.Emit(OpCodes.Ldarg_0)
                        gen_il_load(il, index)
                        il.Emit(OpCodes.Stfld, s.Value.GetField(v.Name))
                    End Sub,
                    s.Key.Initializer.Body,
                    functions,
                    structs
                )
            Next
        End Sub

        Public Overridable Sub DeclareStatement(
                il As ILGenerator,
                gen_il_load As Action(Of ILGenerator, RkValue),
                gen_il_store As Action(Of ILGenerator, RkValue),
                stmts As List(Of RkCode0),
                functions As Dictionary(Of RkFunction, MethodInfo),
                structs As Dictionary(Of RkStruct, TypeData)
            )

            Dim gen_il_3op =
                Sub(ope As OpCode, code As RkCode)

                    gen_il_load(il, code.Left)
                    gen_il_load(il, code.Right)
                    il.Emit(ope)
                    If code.Return IsNot Nothing Then gen_il_store(il, code.Return)
                End Sub

            Dim gen_il_3ad =
                Sub(ope As OpCode, code As RkCode)

                    If code.Left.Type.Name.Equals("String") Then

                        gen_il_load(il, code.Left)
                        gen_il_load(il, code.Right)
                        il.EmitCall(OpCodes.Call, GetType(System.String).GetMethod("Concat", {GetType(String), GetType(String)}), {GetType(String), GetType(String)})
                        If code.Return IsNot Nothing Then gen_il_store(il, code.Return)
                    Else
                        gen_il_3op(ope, code)
                    End If
                End Sub

            Dim get_ctor =
                Function(r As RkStruct)

                    Return Me.RkToCILType(r, structs).Constructor
                End Function

            Dim labels = stmts.Where(Function(x) TypeOf x Is RkLabel).ToHash_ValueDerivation(Function(x) il.DefineLabel)

            Dim found_ret = False
            For Each stmt In stmts

                If TypeOf stmt Is IReturnBind Then

                    Dim bind = CType(stmt, IReturnBind)
                    If TypeOf bind.Return Is RkProperty Then

                        Dim prop = CType(bind.Return, RkProperty)
                        If prop.Receiver IsNot Nothing Then gen_il_load(il, prop.Receiver)
                    End If
                End If

                Select Case stmt.Operator
                    Case RkOperator.Plus : gen_il_3ad(OpCodes.Add, CType(stmt, RkCode))
                    Case RkOperator.Minus : gen_il_3op(OpCodes.Sub, CType(stmt, RkCode))
                    Case RkOperator.Mul : gen_il_3op(OpCodes.Mul, CType(stmt, RkCode))
                    Case RkOperator.Div : gen_il_3op(OpCodes.Div, CType(stmt, RkCode))
                    Case RkOperator.Equal : gen_il_3op(OpCodes.Ceq, CType(stmt, RkCode))

                    Case RkOperator.Bind
                        Dim bind = CType(stmt, RkCode)
                        If bind.Left IsNot Nothing Then

                            gen_il_load(il, bind.Left)
                            gen_il_store(il, bind.Return)
                        End If

                    Case RkOperator.Dot
                        Dim dot = CType(stmt, RkCode)
                        If TypeOf dot.Return.Type Is RkNamespace Then

                            Debug.Fail("not yet")

                        ElseIf TypeOf dot.Left.Type Is RkNamespace AndAlso TypeOf dot.Right.Type Is RkByName Then

                            ' nothing

                        ElseIf TypeOf dot.Left.Type Is RkNamespace AndAlso TypeOf dot.Right.Type Is RkStruct Then

                            ' nothing

                        ElseIf TypeOf dot.Left.Type Is RkStruct AndAlso TypeOf dot.Right.Type Is RkByName Then

                            ' nothing
                        Else

                            gen_il_load(il, dot.Left)
                            il.Emit(OpCodes.Ldfld, Me.RkToCILType(dot.Left.Type, structs).GetField(dot.Right.Name))
                            gen_il_store(il, dot.Return)
                        End If

                    Case RkOperator.Call
                        If TypeOf stmt Is RkLambdaCall Then

                            Dim cc = CType(stmt, RkLambdaCall)
                            Dim bind = Me.RkFunctionToCILType(cc.Function, structs)
                            gen_il_load(il, cc.Value)
                            cc.Arguments.Do(Sub(arg) gen_il_load(il, arg))
                            il.EmitCall(OpCodes.Callvirt, bind.GetMethod("Invoke"), Nothing)
                            If cc.Return IsNot Nothing Then gen_il_store(il, cc.Return)
                        Else
                            Dim cc = CType(stmt, RkCall)
                            cc.Function.Arguments.Do(
                                Sub(x)

                                    If TypeOf x.Value Is RkStruct AndAlso CType(x.Value, RkStruct).ClosureEnvironment Then

                                        gen_il_load(il, New RkValue With {.Name = x.Name, .Type = x.Value})
                                    End If
                                End Sub)
                            cc.Arguments.Do(Sub(arg) gen_il_load(il, arg))
                            If TypeOf cc.Function Is RkCILConstructor Then

                                Dim f = CType(cc.Function, RkCILConstructor)
                                If f.ConstructorInfo Is Nothing Then

                                    Debug.Assert(cc.Arguments.Count = 0, "invalid arguments")
                                    Debug.Assert(cc.Return IsNot Nothing, "invalid return")
                                    Debug.Fail("not yet")
                                    'il.Emit(OpCodes.Ldloca, ) <- cc.Return
                                    il.Emit(OpCodes.Initobj, f.TypeInfo)
                                Else
                                    il.Emit(OpCodes.Newobj, f.ConstructorInfo)
                                End If
                            Else
                                il.Emit(OpCodes.Call, Me.RkToCILFunction(cc.Function, functions, structs))
                            End If
                            If cc.Return IsNot Nothing Then gen_il_store(il, cc.Return)
                        End If

                    Case RkOperator.Return
                        If TypeOf stmt Is RkCode Then gen_il_load(il, CType(stmt, RkCode).Left)
                        il.Emit(OpCodes.Ret)
                        found_ret = True

                    Case RkOperator.Alloc
                        Dim alloc = CType(stmt, RkCode)
                        il.Emit(OpCodes.Newobj, get_ctor(CType(alloc.Left.Type, RkStruct)))
                        gen_il_store(il, alloc.Return)

                    Case RkOperator.Array
                        Dim alloc = CType(stmt, RkCode)
                        Dim array = CType(alloc.Left, RkArray)
                        il.Emit(OpCodes.Newobj, get_ctor(CType(array.Type, RkStruct)))
                        For Each x In array.List

                            il.Emit(OpCodes.Dup)
                            gen_il_load(il, x)
                            il.Emit(OpCodes.Call, Me.RkArrayToCILArray(array.Type, structs).GetMethod("Add"))
                        Next
                        gen_il_store(il, alloc.Return)

                    Case RkOperator.If
                        Dim if_ = CType(stmt, RkIf)
                        gen_il_load(il, if_.Condition)
                        il.Emit(OpCodes.Brtrue, labels(if_.Then))
                        il.Emit(OpCodes.Br, labels(if_.Else))

                    Case RkOperator.Goto
                        il.Emit(OpCodes.Br, labels(CType(stmt, RkGoto).Label))

                    Case RkOperator.Label
                        il.MarkLabel(labels(stmt))


                    Case Else
                        Debug.Fail("not yet")
                End Select
            Next
            If Not found_ret Then il.Emit(OpCodes.Ret)
        End Sub

        Public Overridable Function RkToCILType(r As IType, structs As Dictionary(Of RkStruct, TypeData)) As TypeData

            If r Is Nothing Then Return New TypeData With {.Type = GetType(System.Void), .Constructor = Nothing}
            If TypeOf r Is RkFunction Then Return Me.RkFunctionToCILType(CType(r, RkFunction), structs)
            If TypeOf r Is RkCILStruct Then Return New TypeData With {.Type = CType(r, RkCILStruct).TypeInfo, .Constructor = Nothing}
            If TypeOf r IsNot RkStruct Then Throw New ArgumentException("invalid RkStruct", NameOf(r))
            Return structs(CType(r, RkStruct))
        End Function

        Public Overridable Function RkFunctionToCILType(r As RkFunction, structs As Dictionary(Of RkStruct, TypeData)) As TypeData

            Dim args = r.Arguments.Where(Function(x) Not (TypeOf x.Value Is RkStruct AndAlso CType(x.Value, RkStruct).ClosureEnvironment)).Map(Function(x) Me.RkToCILType(x.Value, structs).Type).ToList
            If r.Return Is Nothing Then

                Dim t = Type.GetType($"System.Action`{args.Count}")
                Dim g = t.MakeGenericType(args.ToArray)
                Return New TypeData With {.Type = g, .Constructor = g.GetConstructor({GetType(Object), GetType(IntPtr)})}
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

        Public Overridable Function RkToCILType(r As List(Of RkValue), structs As Dictionary(Of RkStruct, TypeData)) As System.Type()

            Return r.Map(Function(x) Me.RkToCILType(x.Type, structs).Type).ToArray
        End Function

        Public Overridable Function RkToCILFunction(f As RkFunction, functions As Dictionary(Of RkFunction, MethodInfo), structs As Dictionary(Of RkStruct, TypeData)) As MethodInfo

            If TypeOf f Is RkCILFunction Then Return CType(f, RkCILFunction).MethodInfo
            If functions.ContainsKey(f) Then Return functions(f)

            If Not functions.ContainsKey(f) Then

                ' Array => List
                If f.Arguments.Count > 0 AndAlso f.Arguments(0).Value.Name.Equals("Array") Then

                    Dim arr = Me.RkArrayToCILArray(f.Arguments(0).Value, structs)
                    If f.Name.Equals("[]") Then

                        Return arr.GetMethod("get_Item")
                    End If
                End If
            End If

            Throw New MissingMethodException
        End Function

        Public Overridable Function RkArrayToCILArray(arr As IType, structs As Dictionary(Of RkStruct, TypeData)) As Type

            Return GetType(List(Of )).MakeGenericType(New Type() {Me.RkToCILType(CType(arr, RkStruct).Apply(0), structs).Type})
        End Function
    End Class

End Namespace
