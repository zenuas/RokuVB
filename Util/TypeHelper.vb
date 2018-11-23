Imports System
Imports System.Reflection
Imports Roku.Util.Extensions


Namespace Util

    Public Class TypeHelper

        Public Shared Function IsJust(Of T)(p As Object) As Boolean

            If TypeOf p IsNot T Then Return False
            Return Object.Equals(p.GetType.TypeHandle, GetType(T).TypeHandle)
        End Function

        Public Shared Function IsInterface(p As Type, interf As Type) As Boolean

            Return p Is interf OrElse p.GetTypeInfo.ImplementedInterfaces.FindFirstOrNull(Function(x) x Is interf) IsNot Nothing
        End Function

        Public Shared Function IsGeneric(p As Type, generic As Type) As Boolean

            Return p.IsGenericType AndAlso p.GetTypeInfo.GetGenericTypeDefinition Is generic
        End Function

        Public Overloads Shared Function MemberwiseClone(Of T)(p As T) As T

            Return CType(p.GetType.InvokeMember("MemberwiseClone", BindingFlags.Instance Or BindingFlags.NonPublic Or BindingFlags.InvokeMethod, Nothing, p, Nothing), T)
        End Function

    End Class

End Namespace
