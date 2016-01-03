Imports System
Imports System.Collections.Generic
Imports System.Reflection
Imports Roku.Parser
Imports Roku.Node


<Assembly: AssemblyVersion("0.0.*")> 
Public Class Main

    Public Class Loader

        'Public Overridable Function AddImport(f As String) As Roku.Node.INode
        '    Return Nothing
        'End Function
    End Class

    Public Shared Sub Main(args() As String)

        Dim opt As New Command.Option
        Dim xs = Command.Parser.Parse(opt, args)

        If xs.Length = 0 Then

            CompileConsole(System.Console.In, opt)
        Else

            For Each arg In xs

                CompileFile(arg, opt)
            Next
        End If

#If DEBUG Then
        Console.WriteLine("push any key...")
        Console.ReadKey()
#End If
    End Sub

    Public Shared Sub CompileFile(f As String, opt As Command.Option)

        Using reader As New IO.StreamReader(f)

            Dim parser As New MyParser
            Compile(parser.Parse(New MyLexer(reader) With {.Parser = parser}), opt)
        End Using
        'Compile(loader.AddImport(f).Node)
    End Sub

    Public Shared Sub CompileConsole(reader As System.IO.TextReader, opt As Command.Option)

        Dim parser As New MyParser
        Compile(parser.Parse(New MyLexer(reader) With {.Parser = parser}), opt)
        'Compile((New MyParser).Parse(loader, reader))
    End Sub

    Public Shared Sub Compile(node As INode, opt As Command.Option)

        Compiler.NameResolver.ResolveName(node)
        Compiler.Normalize.Normalization(node)
        Compiler.Closure.Capture(node)
        Dim root = CreateRootNamespace("Global")
        Compiler.Typing.Prototype(node, root)
        Compiler.Typing.TypeInference(node, root)
        Compiler.Translater.Translate(node, root)
        If opt.NodeDump IsNot Nothing Then NodeDumpGraph(opt.NodeDump, node)

    End Sub

    Public Shared Function CreateRootNamespace(name As String) As Manager.RkNamespace

        Return New Manager.SystemLirary With {.Name = name}
    End Function

    Public Shared Sub NodeDumpGraph(out As IO.StreamWriter, node As INode)

        out.WriteLine("
digraph roku {
graph [
];
node [
shape = record,
align = left,
];")

        Util.Traverse.NodesOnce(Of Object)(
            node,
            Nothing,
            Function(parent, ref, child, user, isfirst, next_)

                If TypeOf child Is BlockNode Then

                    Dim block = CType(child, BlockNode)
                    out.WriteLine($"subgraph cluster_block_stmt_{block.GetHashCode} {{")
                    out.WriteLine("  style=solid;")
                    out.WriteLine("  color=black;")
                    out.WriteLine("  node [style=filled];")

                    Dim name = "block"
                    Dim args = ""
                    If parent Is Nothing Then

                        name = "entrypoint"

                    ElseIf TypeOf parent Is FunctionNode AndAlso ref.Equals("Body") Then

                        Dim func = CType(parent, FunctionNode)
                        name = func.Name
                        args = "\l" + String.Join("\l", Util.Functions.Map(func.Arguments, Function(arg) $"{arg.Name.Name} : {arg.Type.Name}"))
                    End If

                    out.WriteLine($"  label = ""{name} ({block.LineNumber}){args}"";")
                    out.WriteLine("  " + String.Join(" -> ", Util.Functions.Map(block.Statements, Function(x) x.GetHashCode.ToString)))
                    out.WriteLine("}")
                End If

                next_(child, user)
                Return child
            End Function)

        Util.Traverse.NodesOnce(Of Object)(
            node,
            Nothing,
            Function(parent, ref, child, user, isfirst, next_)

                If TypeOf child Is BlockNode OrElse
                    TypeOf parent Is FunctionNode OrElse
                    (TypeOf parent Is DeclareNode AndAlso TypeOf child Is TypeNode) OrElse
                    TypeOf child Is FunctionNode Then

                    ' nothing
                Else

                    If isfirst Then

                        Dim name = ""
                        If child.LineNumber.HasValue Then name = $"\l( {child.LineNumber}, {child.LineColumn} )"
                        Select Case True
                            Case TypeOf child Is VariableNode : Dim v = CType(child, VariableNode) : name += $"\l{v.Name}\l`{v.Type?.Name}`"
                            Case TypeOf child Is TypeNode : Dim v = CType(child, TypeNode) : name += $"\l{v.Name}"
                            Case TypeOf child Is NumericNode : Dim v = CType(child, NumericNode) : name += $"\l{v.Numeric}"
                            Case TypeOf child Is StringNode : Dim v = CType(child, StringNode) : name += $"\l{v.String}"
                            Case TypeOf child Is FunctionNode : Dim v = CType(child, FunctionNode) : name += $"\l{v.Name}"
                            Case TypeOf child Is ExpressionNode : Dim v = CType(child, ExpressionNode) : name += $"\l{v.Operator}\l`{v.Type?.Name}`"
                            Case TypeOf child Is DeclareNode : Dim v = CType(child, DeclareNode) : name += $"\l{v.Name.Name}\l`{v.Type?.Name}`"
                            Case TypeOf child Is LetNode : Dim v = CType(child, LetNode) : name += $"\l{v.Var.Name}\l`{v.Type?.Name}`"
                            Case TypeOf child Is StructNode : Dim v = CType(child, StructNode) : name += $"\l{v.Name}\l`{v.Type?.Name}`"
                        End Select

                        out.WriteLine($"{child.GetHashCode} [label = ""{child.GetType.Name}{name}""]")
                    End If

                    If parent IsNot Nothing AndAlso
                        TypeOf parent IsNot BlockNode AndAlso
                        Not (TypeOf parent Is DeclareNode AndAlso TypeOf child Is VariableNode) Then out.WriteLine($"{parent.GetHashCode} -> {child.GetHashCode} [label = ""{ref}""];")
                End If

                next_(child, user)
                Return child
            End Function)

        out.WriteLine("}")
    End Sub

End Class
