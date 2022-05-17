# -*- coding: utf-8 -*-
"""
Created on Sat May  7 15:13:59 2022

@author: pasca
"""

m1 = 2
m2 = 1
v1 = 3
v2 = 0

def impuls(m, v):
    return m * v

def calc_speed_after(m1, m2, v1, v2):
    u1 = (m1 - m2) / (m1 + m2) * v1 + (2*m2) / (m1 + m2) * v2
    u2 = (m2 - m1) / (m1 + m2) * v2 + (2*m1) / (m1 + m2) * v1
    return u1, u2

u1, u2 = calc_speed_after(m1, m2, v1, v2)

print(u1)
print(u2)
    