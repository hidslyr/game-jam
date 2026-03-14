Updating the filling mechanic

1. Piece filling visual update (blendshape) must happen the same time as the pump animation
2. The decreasing amount of basket should divided by pump count, and play smoothly follow pump animation
3. The same as piece filling, whenever the decreasing on basket happen, it should update the filling amount, exactly follow basket decreasing amount.
4. The text amount on piece will display only total amount if current filled amount is 0, otherwise keep it {currentFilledAmount}/{totalAmount}
5. Only current piece and next piece has text amount visible