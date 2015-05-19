data SEGMENT
	val1	dd	3.12
	val2	dq	2.25 
        val3    dd      312
data ENDS 

code SEGMENT
begin:
        FLDZ
	FSUB Val1[edx+esi] 
	FSUB Val2[edx+esi] 
        FSUB gs:Val2[esi+eax]
        FSUB cs:Val3[esi+eax] 
	FMUL ST(0),ST
	FMUL ST(1),ST
	FMUL ST(2),ST
	FMUL ST(3),ST
	FMUL ST(4),ST
        FMUL ST(5),ST
	FMUL ST(6),ST
	FMUL ST(7),ST
        FCOMP st(0)
        FCOMP st(1)
        FCOMP st(2)
        FCOMP st(3)
        FCOMP st(4)
        FCOMP st(5)
        FCOMP st(6)
        FCOMP st(7)

code ENDS 
END begin 
	