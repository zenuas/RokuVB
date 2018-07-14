Namespace IntermediateCode

    Public Enum InOperator

        Nop

        ''' <summary>self context</summary>
        ''' <remarks>1 opcode</remarks>
        Self

        ''' <summary>super class</summary>
        ''' <remarks>2 opcode</remarks>
        Super

        ''' <summary>=</summary>
        ''' <remarks>
        ''' 2 opcode
        ''' Bind x => var
        ''' </remarks>
        Bind

        ''' <summary>dot</summary>
        ''' <remarks>3 opcode</remarks>
        Dot

        ''' <summary>cast</summary>
        ''' <remarks>3 opcode</remarks>
        Cast

        ''' <summary>return</summary>
        ''' <remarks>0 or 1 opcode</remarks>
        [Return]

        ''' <summary>yield</summary>
        ''' <remarks>1 opcode</remarks>
        Yield

        ''' <summary>plus</summary>
        ''' <remarks>non-overflow check</remarks>
        Plus

        ''' <summary>minus</summary>
        ''' <remarks>non-underflow check</remarks>
        Minus

        ''' <summary>uminus</summary>
        ''' <remarks>non-overflow check</remarks>
        UMinus

        ''' <summary>multiple</summary>
        ''' <remarks>non-overflow check</remarks>
        Mul

        ''' <summary>division</summary>
        ''' <remarks>
        ''' Int Div Int => Int
        ''' @T Div 0 => divided by zero exception
        ''' </remarks>
        Div

        ''' <summary>modulo</summary>
        ''' <remarks>
        ''' Int Mod Int => Int
        ''' @T Mod 0 => divided by zero exception
        ''' </remarks>
        [Mod]

        ''' <summary>left shift(logical)</summary>
        ''' <remarks>
        ''' 8bit
        ''' 0b0000_0101 LShift 2 => 0b0001_0100
        ''' 0b0000_0101 LShift 8 => 0b0000_0000
        ''' </remarks>
        LShift

        ''' <summary>right shift(logical)</summary>
        ''' <remarks>
        ''' 8bit
        ''' 0b1001_0100 RShift 2 => 0b0010_0101
        ''' 0b1001_0100 RShift 8 => 0b0000_0000
        ''' </remarks>
        RShift

        ''' <summary>left shift(arithmetic)</summary>
        ''' <remarks>
        ''' 8bit
        ''' 0b1000_0101 LShiftA 2 => 0b1001_0100
        ''' 0b1000_0101 LShiftA 7 => 0b1000_0000
        ''' </remarks>
        LShiftA

        ''' <summary>right shift(arithmetic)</summary>
        ''' <remarks>
        ''' 8bit
        ''' 0b1001_0100 RShiftA 2 => 0b1110_0101
        ''' 0b1001_0100 RShiftA 7 => 0b1111_1111
        ''' </remarks>
        RShiftA

        [Not]
        [And]
        [Or]
        [Xor]

        ''' <summary>===</summary>
        ''' <remarks></remarks>
        Equal

        ''' <summary>&lt;</summary>
        ''' <remarks>less than</remarks>
        Lt

        ''' <summary>&gt;</summary>
        ''' <remarks>greater than</remarks>
        Gt

        ''' <summary>&lt;=</summary>
        ''' <remarks>less than equal</remarks>
        Lte

        ''' <summary>&gt;=</summary>
        ''' <remarks>greater than equal</remarks>
        Gte

        [Alloc]
        [Call]
        [If]
        [Goto]
        Label
        Array
        CanCast
        GetArrayIndex

        [Try]
        [Catch]
        [Finally]
        [Throw]

    End Enum

End Namespace
