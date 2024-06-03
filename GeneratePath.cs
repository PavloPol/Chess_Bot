using Godot;
using System;

public partial class GeneratePath : Node
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public ulong RookPath(int rookPosition, ulong selfBoard, ulong enemyBoard, bool isBlack)
	{
		ulong legalMoves = 0;
		//right
		for(int i = rookPosition + 1; i <= 63 && i%8 != 0; i++)
		{
			if((enemyBoard & (1UL << i)) != 0)
			{
				legalMoves |= 1UL << i;
				break;
			}
			if((selfBoard & (1UL << i)) != 0)
			{
				break;
			}
			legalMoves |= 1UL << i;
		}

		//left
		for(int i = rookPosition - 1; i >= 0 && i%8 != 7; i--)
		{
			if((enemyBoard & (1UL << i)) != 0)
			{
				legalMoves |= 1UL << i;
				break;
			}
			if((selfBoard & (1UL << i)) != 0)
			{
				break;
			}
			legalMoves |= 1UL << i;
		}

		//up
		for(int i = rookPosition + 8; i < 64; i += 8)
		{
			if((enemyBoard & (1UL << i)) != 0)
			{
				legalMoves |= 1UL << i;
				break;
			}
			if((selfBoard & (1UL << i)) != 0)
			{
				break;
			}
			legalMoves |= 1UL << i;
		}

		//down
		for(int i = rookPosition - 8; i >= 0; i -= 8)
		{
			if((enemyBoard & (1UL << i)) != 0)
			{
				legalMoves |= 1UL << i;
				break;
			}
			if((selfBoard &(1UL << i)) != 0)
			{
				break;
			}
			legalMoves |= 1UL << i;
		}
		return legalMoves;
	}

	public ulong KnightPath(int knightPosition, ulong selfBoard, ulong enemyBoard, bool isBlack)
	{
		ulong knightMask = 43234889994;
		int shiftAmount = knightPosition - 18;
		if(shiftAmount >= 0)
		{
			knightMask <<= shiftAmount;
		}
		else
		{
			knightMask >>= -shiftAmount;
		}
		if(knightPosition % 8 > 5)
		{
			knightMask &= ~0x303030303030303UL;
		}
		if(knightPosition % 8 < 2)
		{
			knightMask &= ~0xC0C0C0C0C0C0C0C0UL;
		}
		knightMask ^= selfBoard & knightMask;
		return knightMask;
	}

	public ulong BishopPath(int bishopPosition, ulong selfBoard, ulong enemyBoard, bool isBlack)
	{
		ulong legalMoves = 0;
		//top left
		for(int i = bishopPosition+9; i <= 63 && i%8 != 0; i += 9)
		{
			if((enemyBoard & (1UL << i)) != 0)
			{
				legalMoves |= 1UL << i;
				break;
			}
			if((selfBoard & (1UL << i)) != 0)
			{
				break;
			}
			legalMoves |= 1UL << i;
		}

		//top right
		for(int i = bishopPosition+7; i <= 63 && i%8 != 7;i += 7)
		{
			if((enemyBoard & (1UL << i)) != 0)
			{
				legalMoves |= 1UL << i;
				break;
			}
			if((selfBoard & (1UL << i)) != 0)
			{
				break;
			}
			legalMoves |= 1UL << i;
		}

		//bot left
		for(int i = bishopPosition - 7; i >= 0 && i%8 != 0; i -= 7)
		{
            if ((enemyBoard & (1UL << i)) != 0)
            {
                legalMoves |= 1UL << i;
                break;
            }
            if ((selfBoard & (1UL << i)) != 0)
            {
                break;
            }
            legalMoves |= 1UL << i;
        }

        //bot right
        for (int i = bishopPosition - 9; i >= 0 && i % 8 != 7; i -= 9)
        {
            if ((enemyBoard & (1UL << i)) != 0)
            {
                legalMoves |= 1UL << i;
                break;
            }
            if ((selfBoard & (1UL << i)) != 0)
            {
                break;
            }
            legalMoves |= 1UL << i;
        }
		return legalMoves;
    }

	public ulong QueenPath(int queenPosition, ulong selfBoard, ulong enemyBoard, bool isBlack)
	{
		ulong legalMoves = 0;
		legalMoves = BishopPath(queenPosition, selfBoard, enemyBoard, isBlack) | RookPath(queenPosition, selfBoard, enemyBoard, isBlack);
		return legalMoves;
	}

	public ulong KingPath(int kingPosition, ulong selfBoard, ulong enemyBoard, bool isBlack)
	{
		ulong kingMask = 460039;
		int shiftAmount = kingPosition - 9;
		if(shiftAmount >= 0)
		{
			kingMask <<= shiftAmount;
		}
		else
		{
			kingMask >>= -shiftAmount;
		}
		if(kingPosition % 8 > 6)
		{
			kingMask &= ~0x303030303030303UL;
		}
		if(kingPosition % 8 < 1)
		{
			kingMask &= ~0xC0C0C0C0C0C0C0C0UL;
		}
		kingMask ^= selfBoard & kingMask;
		return kingMask;
	}

	public ulong PawnPath(int pawnPosition, ulong selfBoard, ulong enemyBoard, bool isBlack)
	{
		ulong pawnMask = 0;
		ulong attackMask = 0;
		if (isBlack)
		{
			if(pawnPosition >= 47 && pawnPosition < 56)
			{
				pawnMask = 0x80800000000000;
			}
			else
			{
				pawnMask = 0x80000000000000;
            }
			pawnMask >>= 63 - pawnPosition;
			if(((selfBoard | enemyBoard) & (1UL << (pawnPosition - 8))) != 0)
			{
				pawnMask = 0;
			}
			attackMask = 0xA0000000000000;
			if(pawnPosition == 63)
			{
				attackMask <<= 1;
			}
			else
			{
				attackMask >>= 62 - pawnPosition;
			}
		}
		else
		{
			if(pawnPosition>=7 && pawnPosition < 16)
			{
				pawnMask = 0x10100;
			}
			else
			{
				pawnMask = 0x100;
			}
			attackMask = 0x500;
			pawnMask <<= pawnPosition;
			if(((selfBoard | enemyBoard) & (1UL << (pawnPosition + 8))) != 0)
			{
				pawnMask = 0;
			}
			if(pawnPosition == 0)
			{
				attackMask >>= 1;
			}
			else
			{
				attackMask <<= pawnPosition - 1;
			}
		}
		if(pawnPosition % 8 > 6)
		{
			attackMask &= ~0x303030303030303UL;
        }
		if(pawnPosition % 8 < 1)
		{
            attackMask &= ~0xC0C0C0C0C0C0C0C0UL;
        }
		attackMask &= enemyBoard;
		pawnMask ^= pawnMask&(selfBoard | enemyBoard);
		pawnMask |= attackMask;
		return pawnMask;
	}

}
