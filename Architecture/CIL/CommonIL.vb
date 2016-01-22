Imports System
Imports System.Diagnostics
Imports System.Collections.Generic
Imports System.Reflection
Imports System.Reflection.Emit
Imports Roku.Manager
Imports Roku.Util.ArrayExtension


Namespace Architecture.CIL

    <ArchitectureName("CIL")>
    Public Class CommonIL
        Implements IArchitecture

#Region "export libs"

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
            Public Overridable Property Fields As New Dictionary(Of String, FieldBuilder)

            Public Overridable Function GetField(name As String) As FieldInfo

                Return If(TypeOf Me.Type Is TypeBuilder, Me.Fields(name), Me.Type.GetField(name))
            End Function
        End Class

#End Region

        Public Overridable Property Root As RkNamespace
        Public Overridable Property EntryPoint As String = "Global"
        Public Overridable Property Subsystem As PEFileKinds = PEFileKinds.ConsoleApplication
        Public Overridable Property Assembly As AssemblyBuilder
        Public Overridable Property [Module] As ModuleBuilder
        Public Overridable Property IsDebug As Boolean = True

        Public Overridable Sub Assemble(ns As RkNamespace) Implements IArchitecture.Assemble

            Me.Root = ns

            Dim name As New AssemblyName(Me.EntryPoint)
            Me.Assembly = System.AppDomain.CurrentDomain.DefineDynamicAssembly(name, AssemblyBuilderAccess.Save)
            Me.Module = Me.Assembly.DefineDynamicModule(Me.EntryPoint, System.IO.Path.GetRandomFileName, Me.IsDebug)

            Dim structs = Me.DeclareStructs(Me.Root)
            Dim functions = Me.DeclareMethods(Me.Root, structs)
            Me.DeclareStatements(functions, structs)
            structs.Do(Sub(x) If TypeOf x.Value.Type Is TypeBuilder Then CType(x.Value.Type, TypeBuilder).CreateType())

            If Me.Subsystem <> PEFileKinds.Dll Then

                ' global sub main() {EntryPoint.new();}
                Dim method = Me.Module.DefineGlobalMethod("__EntryPoint", MethodAttributes.Static Or MethodAttributes.Family, GetType(System.Void), System.Type.EmptyTypes)

                Dim il = method.GetILGenerator
                Dim ctor = Util.Functions.These(Function() Me.Root.GetFunction(".ctor"))
                If ctor IsNot Nothing Then il.EmitCall(OpCodes.Call, functions(ctor), System.Type.EmptyTypes)
                'il.Emit(OpCodes.Newobj, Me.Module.GetType(Me.EntryPoint).GetConstructor(System.Type.EmptyTypes))
                'il.Emit(OpCodes.Pop)
                il.Emit(OpCodes.Ret)

                Me.Assembly.SetEntryPoint(method, Me.Subsystem)
            End If
            Me.Module.CreateGlobalFunctions()
        End Sub

        Public Overridable Sub Optimize() Implements IArchitecture.Optimize

            'Throw New NotImplementedException()
        End Sub

        Public Overridable Sub Emit(path As String) Implements IArchitecture.Emit

            Dim temp = System.IO.Path.GetFileName(Me.Module.FullyQualifiedName)
            Dim pdb = System.IO.Path.GetFileNameWithoutExtension(temp) + ".pdb"
            Try
                Me.Assembly.Save(temp)
                System.IO.File.Copy(temp, path, True)
                System.IO.File.Copy(temp, System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path), System.IO.Path.GetFileNameWithoutExtension(path) + ".pdb"), True)

            Finally
                System.IO.File.Delete(temp)
                System.IO.File.Delete(pdb)

            End Try
        End Sub

        Public Overridable Function DeclareStructs(ns As RkNamespace) As Dictionary(Of RkStruct, TypeData)

            Dim map As New Dictionary(Of RkStruct, TypeData)
            map(ns.Structs("Int16")) = New TypeData With {.Type = GetType(Int16), .Constructor = GetType(Int16).GetConstructor(Type.EmptyTypes)}
            map(ns.Structs("Int32")) = New TypeData With {.Type = GetType(Int32), .Constructor = GetType(Int32).GetConstructor(Type.EmptyTypes)}
            map(ns.Structs("Int64")) = New TypeData With {.Type = GetType(Int64), .Constructor = GetType(Int64).GetConstructor(Type.EmptyTypes)}
            map(ns.Structs("String")) = New TypeData With {.Type = GetType(String), .Constructor = GetType(String).GetConstructor(Type.EmptyTypes)}
            For Each struct In ns.Structs.Where(Function(x) x.Value.StructNode IsNot Nothing)

                map(struct.Value) = New TypeData With {.Type = Me.Module.DefineType(struct.Key)}
            Next
            For Each v In map.Where(Function(x) TypeOf x.Value.Type Is TypeBuilder)

                Dim builder = CType(v.Value.Type, TypeBuilder)
                v.Value.Constructor = builder.DefineDefaultConstructor(MethodAttributes.Public)
                For Each x In v.Key.Local

                    v.Value.Fields(x.Key) = builder.DefineField(x.Key, Me.RkStructToCILType(x.Value, map).Type, FieldAttributes.Public)
                Next
            Next
            Return map
        End Function

        Public Overridable Function DeclareMethods(ns As RkNamespace, structs As Dictionary(Of RkStruct, TypeData)) As Dictionary(Of RkFunction, MethodInfo)

            Dim map As New Dictionary(Of RkFunction, MethodInfo)
            For Each fs In ns.Functions

                For Each f In fs.Value.Where(Function(x) Not x.HasGeneric AndAlso x.FunctionNode IsNot Nothing)

                    Dim args = Me.RkStructToCILType(f.Arguments, structs)
                    Debug.Assert(f.Body.Count > 0, $"{f} statement is nothing")
                    map(f) = Me.Module.DefineGlobalMethod(f.CreateManglingName, MethodAttributes.Static Or MethodAttributes.Public, Me.RkStructToCILType(f.Return, structs).Type, args)
                Next

                For Each f In fs.Value.Where(Function(x) Not x.HasGeneric AndAlso x.FunctionNode Is Nothing)

                    Dim args = Me.RkStructToCILType(f.Arguments, structs)
                    Dim export = Me.Exports.Where(Function(x) f.Name.Equals(x.Name) AndAlso args.And(Function(arg, i) arg Is x.Arguments(i)))
                    If Not export.IsNull Then

                        Dim e = export.Car
                        map(f) = System.Reflection.Assembly.Load(e.Assembly).GetType(e.Class).GetMethod(e.Method, e.Arguments)
                    End If
                Next
            Next

            Return map
        End Function

        Public Overridable Sub DeclareStatements(functions As Dictionary(Of RkFunction, MethodInfo), structs As Dictionary(Of RkStruct, TypeData))

            For Each f In functions.Where(Function(x) TypeOf x.Value Is MethodBuilder)

                Dim il = CType(f.Value, MethodBuilder).GetILGenerator
                Dim locals = f.Key.Arguments.Map(Function(x) x.Name).ToHash_ValueDerivation(Function(x, i) -i - 1)
                Dim max_local = 0

                Dim get_local =
                    Function(v As RkValue)

                        Dim name = v.Name
                        If locals.ContainsKey(name) Then Return locals(name)
                        il.DeclareLocal(Me.RkStructToCILType(v.Type, structs).Type).SetLocalSymInfo(name)
                        locals(name) = max_local
                        max_local += 1
                        Return max_local - 1
                    End Function

                Dim gen_il_load =
                    Sub(v As RkValue)

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

                        ElseIf TypeOf v Is RkValue Then

                            Dim index = get_local(v)
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
                        Else
                        End If
                    End Sub

                Dim gen_il_store =
                    Sub(v As RkValue)

                        Dim index = get_local(v)
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

                Dim gen_il_3op =
                    Sub(ope As OpCode, code As RkCode)

                        gen_il_load(code.Left)
                        gen_il_load(code.Right)
                        il.Emit(ope)
                        If code.Return IsNot Nothing Then gen_il_store(code.Return)
                    End Sub

                Dim gen_il_3ad =
                    Sub(ope As OpCode, code As RkCode)

                        If code.Left.Type.Name.Equals("String") Then

                            gen_il_load(code.Left)
                            gen_il_load(code.Right)
                            il.EmitCall(OpCodes.Call, GetType(System.String).GetMethod("Concat", {GetType(String), GetType(String)}), {GetType(String), GetType(String)})
                            If code.Return IsNot Nothing Then gen_il_store(code.Return)
                        Else
                            gen_il_3op(ope, code)
                        End If
                    End Sub

                Dim get_ctor =
                    Function(t As IType)

                        Dim r = CType(t, RkStruct)
                        Return Me.RkStructToCILType(r, structs).Constructor
                    End Function

                Dim labels = f.Key.Body.Where(Function(x) TypeOf x Is RkLabel).ToHash_ValueDerivation(Function(x) il.DefineLabel)

                Dim found_ret = False
                For Each stmt In f.Key.Body

                    Select Case stmt.Operator
                        Case RkOperator.Plus : gen_il_3ad(OpCodes.Add, CType(stmt, RkCode))
                        Case RkOperator.Minus : gen_il_3op(OpCodes.Sub, CType(stmt, RkCode))
                        Case RkOperator.Mul : gen_il_3op(OpCodes.Mul, CType(stmt, RkCode))
                        Case RkOperator.Div : gen_il_3op(OpCodes.Div, CType(stmt, RkCode))
                        Case RkOperator.Equal : gen_il_3op(OpCodes.Ceq, CType(stmt, RkCode))

                        Case RkOperator.Bind
                            Dim bind = CType(stmt, RkCode)
                            gen_il_load(bind.Left)
                            gen_il_store(bind.Return)

                        Case RkOperator.Dot
                            Dim dot = CType(stmt, RkCode)
                            gen_il_load(dot.Left)
                            il.Emit(OpCodes.Ldfld, Me.RkStructToCILType(dot.Left.Type, structs).GetField(dot.Right.Name))
                            gen_il_store(dot.Return)

                        Case RkOperator.Call
                            Dim cc = CType(stmt, RkCall)
                            cc.Arguments.Do(Sub(arg) gen_il_load(arg))
                            il.Emit(OpCodes.Call, functions(cc.Function))
                            If cc.Return IsNot Nothing Then gen_il_store(cc.Return)

                        Case RkOperator.Return
                            If TypeOf stmt Is RkCode Then gen_il_load(CType(stmt, RkCode).Left)
                            il.Emit(OpCodes.Ret)
                            found_ret = True

                        Case RkOperator.Alloc
                            Dim alloc = CType(stmt, RkCode)
                            il.Emit(OpCodes.Newobj, get_ctor(alloc.Left.Type))
                            gen_il_store(alloc.Return)

                        Case RkOperator.If
                            Dim if_ = CType(stmt, RkIf)
                            gen_il_load(if_.Condition)
                            il.Emit(OpCodes.Brtrue, labels(if_.Then))
                            il.Emit(OpCodes.Br, labels(if_.Else))

                        Case RkOperator.Goto
                            il.Emit(OpCodes.Br, labels(CType(stmt, RkGoto).Label))

                        Case RkOperator.Label
                            il.MarkLabel(labels(stmt))

                    End Select
                Next
                If Not found_ret Then il.Emit(OpCodes.Ret)
            Next
        End Sub

        Public Overridable Function RkStructToCILType(r As IType, structs As Dictionary(Of RkStruct, TypeData)) As TypeData

            If r Is Nothing Then Return New TypeData With {.Type = GetType(System.Void), .Constructor = Nothing}
            If TypeOf r IsNot RkStruct Then Throw New ArgumentException("invalid RkStruct", NameOf(r))
            Return structs(CType(r, RkStruct))
        End Function

        Public Overridable Function RkStructToCILType(r As List(Of NamedValue), structs As Dictionary(Of RkStruct, TypeData)) As System.Type()

            Return r.Map(Function(x) Me.RkStructToCILType(x.Value, structs).Type).ToArray
        End Function
    End Class

End Namespace
