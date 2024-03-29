﻿Imports System
Imports Roku.Node
Imports Roku.Util.Extensions


Namespace Parser

    Partial Public Class MyParser

        Public Overridable Property Loader As Loader

#Region "scope"

        Public Overridable Property CurrentScope As IScopeNode

        Public Overridable Sub PushScope(scope As IScopeNode)

            scope.Parent = Me.CurrentScope
            Me.CurrentScope = scope
        End Sub

        Public Overridable Function PopScope() As IScopeNode

            Dim prev_scope = Me.CurrentScope
            Me.CurrentScope = Me.CurrentScope.Parent
            Return prev_scope
        End Function

#End Region

        Public Shared Function AppendLineNumber(Of T As BaseNode)(node As T, token As Token) As T

            node.AppendLineNumber(token)
            Return node
        End Function

        Public Shared Sub AddUse(parser As MyParser, use As UseNode)

            use.Module = parser.Loader.AddUse(use.GetNamespace)
            CType(parser.CurrentScope, ProgramNode).Uses.Add(use)
        End Sub

        Public Shared Function CreateLetNode(
                var As VariableNode,
                expr As IEvaluableNode,
                Optional local_var As Boolean = False,
                Optional user_definition As Boolean = True
            ) As LetNode

            var.LocalVariable = local_var
            Dim let_ As New LetNode With {.Var = var, .Expression = expr, .UserDefinition = user_definition}
            let_.AppendLineNumber(var)
            Return let_
        End Function

        Public Shared Function CreateLetNode(
                prop As PropertyNode,
                expr As IEvaluableNode,
                Optional user_definition As Boolean = True
            ) As LetNode

            Dim let_ As New LetNode With {.Receiver = prop.Left, .Var = prop.Right, .Expression = expr, .UserDefinition = user_definition}
            let_.AppendLineNumber(prop)
            Return let_
        End Function

        Public Shared Function CreateLetNode(
                var As VariableNode,
                type As TypeBaseNode,
                Optional local_var As Boolean = False
            ) As LetNode

            var.LocalVariable = local_var
            Dim let_ As New LetNode With {.Var = var, .Declare = type}
            let_.AppendLineNumber(var)
            Return let_
        End Function

        Public Shared Function CreateLetNode(
                var As VariableNode,
                type As TypeBaseNode,
                expr As IEvaluableNode,
                Optional local_var As Boolean = False
            ) As LetNode

            var.LocalVariable = local_var
            Dim let_ As New LetNode With {.Var = var, .Declare = type, .Expression = expr}
            let_.AppendLineNumber(var)
            Return let_
        End Function

        Public Shared Function CreateLetNode(
                vars As ListNode(Of LetNode),
                expr As IEvaluableNode
            ) As LetNode

            Dim let_ As New LetNode With {.Receiver = vars, .Expression = expr, .TupleAssignment = True}
            If vars.List.Count > 0 Then let_.AppendLineNumber(vars.List(0))
            Return let_
        End Function

        Public Shared Function CreateIgnoreLetNode(
                ignore As Token
            ) As LetNode

            Dim let_ As New LetNode With {.Var = CreateVariableNode("_", ignore), .IsIgnore = True}
            let_.AppendLineNumber(ignore)
            Return let_
        End Function

        Public Shared Function CreateFunctionCallNode(
                expr As IEvaluableNode,
                ParamArray args() As IEvaluableNode
            ) As FunctionCallNode

            Dim fcall As New FunctionCallNode With {.Expression = expr, .Arguments = args}
            fcall.AppendLineNumber(expr)
            Return fcall
        End Function

        Public Shared Function CreateFunctionCallNode(
                ope As Token,
                ParamArray args() As IEvaluableNode
            ) As FunctionCallNode

            Dim expr = CreateVariableNode(ope)
            expr.AppendLineNumber(args(0))
            Dim fcall = CreateFunctionCallNode(expr, args)
            If args.Length = 1 Then fcall.UnaryOperator = True
            Return fcall
        End Function

        Public Shared Function CreatePropertyNode(
                left As IEvaluableNode,
                dot As Token,
                right As VariableNode
            ) As PropertyNode

            Dim prop As New PropertyNode With {.Left = left, .Right = right}
            If dot IsNot Nothing Then prop.AppendLineNumber(dot)
            Return prop
        End Function

        Public Shared Function CreateIfExpressionNode(
                cond As IEvaluableNode,
                [then] As IEvaluableNode,
                [else] As IEvaluableNode
            ) As IfExpressionNode

            Dim expr As New IfExpressionNode With {.Condition = cond, .Then = [then], .Else = [else]}
            expr.AppendLineNumber(cond)
            Return expr
        End Function

        Public Shared Function CreateTupleNode(
                items As ListNode(Of IEvaluableNode)
            ) As TupleNode

            Dim tuple As New TupleNode With {.Items = items.List.ToArray}
            tuple.AppendLineNumber(items)
            Return tuple
        End Function

        Public Shared Function CreateListNode(Of T As INode)() As ListNode(Of T)

            Return New ListNode(Of T)
        End Function

        Public Function CreateListNode(Of T As INode)(ParamArray expr() As T) As ListNode(Of T)

            Dim list = CreateListNode(Of T)()
            list.List.AddRange(expr)

            Return list
        End Function

        Public Shared Function CreateVariableNode(s As Token) As VariableNode

            Return CreateVariableNode(s.Name, s)
        End Function

        Public Shared Function CreateVariableNode(s As String, pos As Token) As VariableNode

            If TypeOf pos.Value Is VariableNode Then Return CType(pos.Value, VariableNode)

            Dim var_ = New VariableNode(s)
            var_.AppendLineNumber(pos)

            Return var_
        End Function

        Public Shared Function CreateVariableNode(s As String, node As INode) As VariableNode

            Dim var_ = New VariableNode(s)
            var_.AppendLineNumber(node)

            Return var_
        End Function

        Public Shared Function CreateIfNode(
                cond As IEvaluableNode,
                [then] As BlockNode
            ) As IfNode

            Return CreateIfNode(cond, [then], Nothing)
        End Function

        Public Shared Function CreateIfNode(
                cond As IEvaluableNode,
                [then] As BlockNode,
                [else] As BlockNode
            ) As IfNode

            If [then] IsNot Nothing Then [then].InnerScope = True
            If [else] IsNot Nothing Then [else].InnerScope = True
            Dim [if] As New IfNode With {.Condition = cond, .Then = [then], .Else = [else]}
            [if].AppendLineNumber(cond)
            Return [if]
        End Function

        Public Shared Function CreateIfCastNode(
                var As VariableNode,
                decla As TypeBaseNode,
                cond As IEvaluableNode,
                [then] As BlockNode
            ) As IfCastNode

            If [then] IsNot Nothing Then [then].InnerScope = True
            Dim [if] As New IfCastNode With {.Condition = cond, .Then = [then], .Var = var, .Declare = decla}
            [if].AppendLineNumber(var)
            Return [if]
        End Function

        Public Shared Function AddElse(
                [if] As IfNode,
                [else] As BlockNode
            ) As IfNode

            If [if].Else Is Nothing Then

                [if].Else = [else]
            Else

                AddElse(CType([if].Else.Statements(0), IfNode), [else])
            End If
            Return [if]
        End Function

        Public Shared Function CreateSwitchNode([case] As ICaseNode) As SwitchNode

            Dim switch As New SwitchNode
            AddSwitchCase(switch, [case])
            switch.AppendLineNumber([case])
            Return switch
        End Function

        Public Shared Sub AddSwitchCase(switch As SwitchNode, [case] As ICaseNode)

            If TypeOf [case] Is CaseValueNode Then

                Dim expr = CType(CType([case], CaseValueNode).Value.Statements(0), LetNode).Expression
                If TypeOf expr Is FunctionCallNode AndAlso CType(expr, FunctionCallNode).UnaryOperator Then

                    CType(expr, FunctionCallNode).OwnerSwitchNode = switch
                End If
            End If

            switch.Case.Add([case])
        End Sub

        Public Shared Function CreateCaseValueNode(block As BlockNode) As CaseValueNode

            Dim [case] As New CaseValueNode With {.Value = block}
            [case].AppendLineNumber(block)
            Return [case]
        End Function

        Public Shared Function CreateCaseCastNode(
                decla As TypeBaseNode,
                var As VariableNode
            ) As CaseCastNode

            Dim [case] As New CaseCastNode With {.Declare = decla, .Var = var}
            [case].AppendLineNumber(decla)
            Return [case]
        End Function

        Public Shared Function CreateCaseArrayNode(
                pattern As ListNode(Of VariableNode),
                token As Token
            ) As CaseArrayNode

            Dim [case] As New CaseArrayNode With {.Pattern = pattern.List}
            [case].AppendLineNumber(token)
            Return [case]
        End Function

        Public Shared Function CreateClassNode(
                name As VariableNode,
                args As ListNode(Of TypeBaseNode),
                conds As ListNode(Of FunctionNode)
            ) As ClassNode

            Dim class_ As New ClassNode(name.LineNumber.Value)
            class_.Name = name.Name
            class_.Generics.AddRange(args.List)
            conds.List.Each(Sub(x) class_.AddFunction(x))
            Return class_
        End Function

        Public Shared Function CreateFunctionNode(
                f As FunctionNode,
                name As VariableNode,
                args As ListNode(Of DeclareNode),
                ret As TypeBaseNode,
                where As ListNode(Of TypeBaseNode)
            ) As FunctionNode

            f.Name = name.Name
            f.Arguments = args.List
            f.Return = ret
            f.InnerScope = False
            If where IsNot Nothing Then

                where.List.By(Of TypeNode).Each(
                    Sub(x)

                        x.IsTypeClass = True
                        f.Where.Add(x)
                    End Sub)
            End If
            Return f
        End Function

        Public Shared Function CreateFunctionNode(
                f As FunctionNode,
                args As ListNode(Of DeclareNode),
                ret As TypeBaseNode
            ) As FunctionNode

            args.List.Each(Sub(x, i) If x.Type Is Nothing Then x.Type = New TypeNode With {.Name = $"#{i}", .IsGeneric = True})
            f.Name = $"#{f.LineNumber},{f.LineColumn}"
            f.Arguments = args.List
            f.Return = ret
            f.InnerScope = False
            Return f
        End Function

        Public Shared Function CreateFunctionNode(
                name As VariableNode,
                args As ListNode(Of DeclareNode),
                ret As TypeBaseNode,
                where As ListNode(Of TypeBaseNode)
            ) As FunctionNode

            Return CreateFunctionNode(New FunctionNode(name.LineNumber.Value), name, args, ret, where)
        End Function

        Public Shared Function CreateFunctionNode(
                name As VariableNode,
                args As ListNode(Of TypeBaseNode),
                ret As TypeBaseNode,
                where As ListNode(Of TypeBaseNode)
            ) As FunctionNode

            Dim xs As New ListNode(Of DeclareNode)
            xs.List.AddRange(args.List.Map(Function(x, i) New DeclareNode(CreateVariableNode($"arg{i}", x), x)))
            Return CreateFunctionNode(New FunctionNode(name.LineNumber.Value), name, xs, ret, where)
        End Function

        Public Shared Function CreateFunctionTypeNode(
                args As ListNode(Of TypeBaseNode),
                ret As TypeBaseNode,
                token As Token
            ) As TypeFunctionNode

            Dim t As New TypeFunctionNode
            t.Arguments = args.List
            t.Return = ret
            t.AppendLineNumber(token)
            Return t
        End Function

        Public Shared Function CreateLambdaFunction(
                scope As IScopeNode,
                f As FunctionNode,
                args As ListNode(Of DeclareNode),
                ret As TypeBaseNode
            ) As VariableNode

            f = CreateFunctionNode(f, If(args, New ListNode(Of DeclareNode)), ret)
            Dim v = New VariableNode(f.Name)
            v.AppendLineNumber(f)
            scope.Lets.Add(f.Name, f)
            Return v
        End Function

        Public Shared Function CreateImplicitLambdaFunction(
                scope As IScopeNode,
                f As FunctionNode,
                args As ListNode(Of DeclareNode),
                ret As TypeNode
            ) As VariableNode

            f = CreateFunctionNode(f, If(args, New ListNode(Of DeclareNode)), ret)
            If args Is Nothing OrElse args.List.Count = 0 Then f.ImplicitArgumentsCount = 0
            If ret Is Nothing Then f.ImplicitReturn = (f.Statements.Count > 0 AndAlso TypeOf f.Statements(0) Is LambdaExpressionNode)
            Dim v = New VariableNode(f.Name)
            v.AppendLineNumber(f)
            scope.Lets.Add(f.Name, f)
            Return v
        End Function

        Public Shared Function ToLambdaExpression(scope As IScopeNode, expr As IEvaluableNode) As FunctionNode

            Dim f = New FunctionNode(expr.LineNumber.Value)
            Dim lambda = New LambdaExpressionNode With {.Expression = expr}
            f.AppendLineNumber(expr)
            lambda.AppendLineNumber(expr)
            f.Statements.Add(lambda)
            f.Parent = scope
            Return f
        End Function

        Public Shared Function ToLambdaExpressionBlock(scope As IScopeNode, expr As IEvaluableNode) As BlockNode

            Dim block = New BlockNode(expr.LineNumber.Value)
            Dim lambda = New LambdaExpressionNode With {.Expression = expr}
            lambda.AppendLineNumber(expr)
            block.Statements.Add(lambda)
            block.Parent = scope
            Return block
        End Function

        Public Shared Function ToBlock(scope As IScopeNode, stmt As IStatementNode) As BlockNode

            Dim block = New BlockNode(stmt.LineNumber.Value)
            block.Statements.Add(stmt)
            block.Parent = scope
            Return block
        End Function

        Public Shared Function ToStatementBlock(scope As IScopeNode, expr As IEvaluableNode) As BlockNode

            Return ToBlock(scope, CType(expr, IStatementNode))
        End Function

        Public Shared Function ToLetBlock(scope As IScopeNode, expr As IEvaluableNode) As BlockNode

            Return ToBlock(scope, CType(CreateLetNode(CreateVariableNode("$ret", expr), expr, False, False), IStatementNode))
        End Function

        Public Shared Function ExpressionToType(expr As IEvaluableNode, t1 As IEvaluableNode, ParamArray ts As TypeBaseNode()) As TypeNode

            Dim check_type =
                Function(t As TypeBaseNode) As TypeNode

                    If TypeOf t IsNot TypeNode Then SyntaxError(t, "not type")
                    Return CType(t, TypeNode)
                End Function

            Dim to_type As Func(Of IEvaluableNode, TypeBaseNode) =
                Function(e)

                    If TypeOf e Is TypeBaseNode Then

                        Return CType(e, TypeBaseNode)

                    ElseIf TypeOf e Is VariableNode Then

                        Return New TypeNode(CType(e, VariableNode))

                    ElseIf TypeOf e Is PropertyNode Then

                        Dim prop = CType(e, PropertyNode)
                        Return New TypeNode(check_type(to_type(prop.Left)), prop.Right)
                    End If

                    SyntaxError(expr, "not type")
                    Return Nothing
                End Function

            Dim type = check_type(to_type(expr))
            type.Arguments.Add(to_type(t1))
            type.Arguments.AddRange(ts)
            Return type
        End Function

        Public Shared Sub SyntaxError(t As Token, Optional message As String = "syntax error")

            Throw New SyntaxErrorException(t.LineNumber, t.LineColumn, message)
        End Sub

        Public Shared Sub SyntaxError(t As INode, Optional message As String = "syntax error")

            Throw New SyntaxErrorException(If(t.LineNumber, 0), If(t.LineColumn, 0), message)
        End Sub

    End Class

End Namespace
