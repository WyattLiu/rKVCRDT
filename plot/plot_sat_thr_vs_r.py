import matplotlib.pyplot as plt
import numpy as np
import time
import sys
import re
from collections import defaultdict
import statistics
import glob
import os
from numpy.polynomial.polynomial import polyfit
from latency_analyzer import *
from scipy.interpolate import make_interp_spline, BSpline

def lookup(xoverride_path_, file_name):
    f = open(xoverride_path_, "r")
    lines = f.readlines()
    for line in lines:
        cols = line.split()
        if(cols[1] == file_name):
            f.close()
            return cols[0].split(',')
    f.close()

def plot_one_dir(dir_, label_, xoverride_path_):
    pts = []
    for file_ in os.listdir(dir_):
        if file_.endswith("_tp.csv"):
            print(file_)
            f = open(dir_ + "/" + file_,"r")
            lines = f.readlines()
            throughput = lines[1].split(',')[1].strip()
            f.close()
            x_override = lookup(xoverride_path_, dir_ + "/" + file_)
            print("(" + str(x_override) + "," + str(throughput) + ")")
            for x in x_override:
                pts.append((int(x), float(throughput)))
    return pts

if __name__ == "__main__":
    if len(sys.argv) < 3:
        raise ValueError('wrong arg')
    print("Data dir: " + sys.argv[1])
    print("Override x dir: " + sys.argv[2])
    xoverride_path = sys.argv[2]
    fig = plt.figure()
    ax1 = fig.add_subplot(111)
    #plt.xlim((0,100000))
    #plt.ylim((0,1000))
    plt.ylabel("Throughput (ops/s)")
    plt.xlabel("% reversed")
    plt.grid(True)
    #plt.yscale('log')

    with open(sys.argv[1]) as file:
            lines = file.readlines()
    for line in lines:
        cols = line.split()
        print("Dir " + cols[0] + " label: " + cols[1] + " style: " + cols[2])
        pts = plot_one_dir(cols[0], cols[1], xoverride_path)
        print(pts)
        pts.sort(key=lambda tup: tup[0])
        x = [item[0] for item in pts]
        y = [item[1] for item in pts]
        print("data: " + str(x))
        print("data: " + str(y))
        x = np.array(x)
        y = np.array(y)
        style = cols[2].split(",")
        ax1.scatter(x, y, s=10, marker=style[0], color = style[1])
        newx = np.linspace(x.min(), x.max(), 300) 
        spl = make_interp_spline(x, y, k=1)
        smooth = spl(newx)
        ax1.plot(newx, smooth, color = style[1], linestyle = style[2], label = str(cols[1]))

    plt.legend(loc='best');
    plt.savefig("thrput_vs_r.png", dpi = 300)

