def calc_nice(GA):
    if GA >= 38:
        return [100, 125, 150, 175, 200, 212, 225, 237, 250, 262, 275, 287, 300, 312, 325, 337, 350], [100, 150, 200, 250, 300, 350, 400, 450, 450, 450, 450, 450, 450, 450, 450, 450, 450]
    
    # According to the formulas:
    # Row 2 (0 hrs): D2 = 40, E2 = 80? Wait, let's look at the formulas again:
    # D2 = 40. E2 = 80.
    # D15 (which is 78 hours? let's count: 0, 6, 12, 18, 24, 30, 36, 42, 48, 54, 60, 66, 72)
    # Wait, the formula for D15 is: GA * 10 - 100.
    # D14 is: ... wait, let me look at row 15 in the python output.
