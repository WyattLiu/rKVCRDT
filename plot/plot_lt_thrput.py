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

def plot_one_dir(dir_, label_):
    pts = []
    for file_ in os.listdir(dir_):
        if file_.endswith("_lt.txt"):
            csv_file = file_.replace("_lt.txt", "_tp.csv")
            print(file_)
            print(csv_file)
            median_lt = (latency_analyzer(dir_+"/"+file_).get_res())[-1]
            cmd = "cat " + dir_+"/" + csv_file + " | tail -n 1 | sed 's/,/ /g' | awk '{print $2}'"
            stream = os.popen(cmd)
            throughput = stream.read().strip()
            print("(" + str(median_lt) + "," + str(throughput) + ")")
            pts.append((median_lt, float(throughput)))
    return pts
if __name__ == "__main__":
    if len(sys.argv) < 2:
        raise ValueError('wrong arg')
    print("Data dir: " + sys.argv[1])
    fig = plt.figure()
    ax1 = fig.add_subplot(111)
    #plt.xlim((0,100000))
    #plt.ylim((0,1000))
    plt.xlabel("Throughput (ops/s)")
    plt.ylabel("Median Latency (ms)")
    plt.grid(True)
    #ax1.set_xscale('log')
    #ax1.set_yscale('log')

    plt.xscale('log')
    plt.yscale('log')

    with open(sys.argv[1]) as file:
            lines = file.readlines()
    for line in lines:
        cols = line.split()
        print("Dir " + cols[0] + " label: " + cols[1] + " style: " + cols[2])
        pts = plot_one_dir(cols[0], cols[1])
        x = [item[1] for item in pts]
        y = [item[0] for item in pts]
        print("data: " + str(x))
        print("data: " + str(y))
        x.sort()
        y.sort()
        x = np.array(x)
        y = np.array(y)
        style = cols[2].split(",")
        ax1.scatter(x, y, s=10, marker=style[0], label=str(cols[1]), color = style[1])
        newx = np.linspace(x.min(), x.max(), 300) 
        spl = make_interp_spline(x, y, k=1)
        smooth = spl(newx)
        ax1.plot(newx, smooth, color = style[1], linestyle = style[2])

    plt.legend(loc='best');
    plt.savefig("lt-thr-plot.png", dpi = 300)

