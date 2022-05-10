import matplotlib.pyplot as plt
import numpy as np
import time
import sys
import re
from collections import defaultdict
import statistics
from numpy.polynomial.polynomial import polyfit
from scipy.interpolate import make_interp_spline, BSpline


from latency_analyzer import *

if __name__ == "__main__":
    
    if len(sys.argv) < 2:
        raise ValueError('wrong arg')
    with open(sys.argv[1]) as file:
        lines = file.readlines()
    fig = plt.figure()
    ax1 = fig.add_subplot(111)
    
    for line in lines:
        cols = line.split()
        print("lt file: " + cols[0] + " label: " + cols[1] + " args: " + cols[2])
        args = cols[2].split(',')
        total_points = args[0]
        window_size = args[1]
        # halfed on each side, say 1s meaning the time before and after
        print("Parsing the args:" + " will have " + str(total_points) + " data points, window size: " + str(window_size) + "then plot accordingly")
        la_obj = latency_analyzer(cols[0])
        span = la_obj.get_span()
        from_time = span[0]
        to_time = span[1]
        slice_interval = float(to_time - from_time)/float(total_points)
        time_span = slice_interval * float(window_size)
        print("Time zero is now: " + str(from_time))
        list_of_throughput = []
        time = from_time + slice_interval * 0.5
        while(time < to_time):
            ops = la_obj.get_ops_of_time(time, time_span)
            thrput_per_s = len(ops) * 1000000000 / time_span
            list_of_throughput.append(((time - from_time)/(to_time - from_time) * 100, thrput_per_s))
            time += slice_interval
        x = [item[0] for item in list_of_throughput]
        y = [item[1] for item in list_of_throughput]
        x = np.array(x)
        y = np.array(y)
        ax1.scatter(x, y, s=10, marker="o", label=cols[1])
        newx = np.linspace(x.min(), x.max(), 300) 
        spl = make_interp_spline(x, y, k=1)
        smooth = spl(newx)
        ax1.plot(newx, smooth)

    plt.xlabel("Time elapsed %")
    plt.ylabel("Throughput (ops/s)")
    plt.grid(True)
    plt.legend(loc='best');
    plt.savefig("./thrput_series_p.png", dpi = 300)

