![alt tag](/Misc/make.png?raw=true)

Make is a prototyping tool for virtual reality, built for artists. 

<img src="/Misc/progress.gif" width=400px>

## To do
- [x] Fix plane/edge gaps - must be in plane
- [x] Have to delete blocks when changing next plane - in modify()
- Even after fixing plane gaps, there are still gaps in 3d surface
- This might be because using Bresenham's for lines doesn't create perfect planes
- Also might be because of the way you delete previously set planes

- [ ] Check fillposlist stuff
- Don't remove completing edges from it
- [ ] Revisit algorithm
- [ ] Fill in volume

- Have to refactor this code at some point

- After this - planes, then done building for a bit, then interaction triggers, then painting and styling
- [ ] Can do stylistic fog for fun
