1. Adjust level format to support empty slot in grid

For example, old format for grid

[G-2, G-2, G-2]
[R-4, R-2]
[B-6]

New format for grid

[G-2, G-2, G-2]
[R-4, R-2, E]
[B-6, E, E]

Where E is empty slot

Make sure where

[G-2, G-2, G-2]
[R-4, R-2, E]
[E, B-6, E]

The B-6 need to sit on middle column, and moving upward by that column

2. Reading and apply level data from googlesheet

- Reading data from googlesheet
    - sheet ID: 7mB6worBegQ2Xgi4qXcDpKXYX5M-4twZVDkbrg17KeU
    - Level 1 gid: 0
    - Level 2 gid: 129557841
    - Level 3 gid: 129557841

A context menu option to read the data, and apply to current level data object.