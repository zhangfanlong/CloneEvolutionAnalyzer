#!/usr/bin/python
# -*- coding: cp936 -*-
import re, sys, os
import xml.dom.minidom
import math

#C:\Users\founder\extract.py E:\论文资料\5实验系统\wget-results\CRDFiles\blocks\ E:\论文资料\5实验系统\wget-results\MAPFiles\blocks\
nclones = []
DNAME = []
CID = []
SID = []
LOC = []
DBT = []
SIM = []
NOP = []
CTX = []
NOT = []
NOD = []
UOT = []
UOD = []
LEN = []
VCB = []
VOL = []
LV = []
DIF = []
CON = []
EFF = []
PT = []
EP  = []
AGE = []
CC  = []
TG  = []
STATIC = []
SAME = []
ADD = []
DELETE = []
SPLIT = []
file_list1 = os.listdir(sys.argv[1])
def num_of_clones(a, b):
        file_list = file_list1[a:b]
        num = 0
        for filename in file_list:
                doc = xml.dom.minidom.parse(sys.argv[1] + filename) 
                root = doc.documentElement
                class_nodes = root.getElementsByTagName('class')
                for classNode in class_nodes:
                        num = num + int(classNode.getAttribute('nclones'))
        return num
for filename1 in file_list1:
	doc1 = xml.dom.minidom.parse(sys.argv[1] + filename1) 
	root1 = doc1.documentElement
	source_nodes = root1.getElementsByTagName('source')		
	for sourceNode in source_nodes:			
		e = int(sourceNode.getAttribute('endline'))
		s = int(sourceNode.getAttribute('startline'))
		LOC.append(str(e - s))
		uotNode = sourceNode.getElementsByTagName('UniqueOprator')[0]
		uotvalue = int(uotNode.childNodes[0].nodeValue)
		UOT.append(str(uotvalue))
		uodNode = sourceNode.getElementsByTagName('UniqueOprand')[0]
		uodvalue = int(uodNode.childNodes[0].nodeValue)
		UOD.append(str(uodvalue))
		notNode = sourceNode.getElementsByTagName('TotalOprator')[0]
		notvalue = int(notNode.childNodes[0].nodeValue)
		NOT.append(str(notvalue))
		nodNode = sourceNode.getElementsByTagName('TotalOprand')[0]
		nodvalue = int(nodNode.childNodes[0].nodeValue)
		NOD.append(str(nodvalue))
		N = notvalue + nodvalue
		C = uotvalue + uodvalue
		V = N * math.log(float(C), 2)
		L = (2 * uodvalue) / float(uotvalue * nodvalue)
		D = 1 / float(L)
		I = L * V
		E = V * D
		T = float(E) / 18
		LEN.append(str(N))
		VCB.append(str(C))
		VOL.append(str(V))
		LV.append(str(L))
		DIF.append(str(D))
		CON.append(str(I))
		EFF.append(str(E))
		PT.append(str(T))
		if sourceNode.getElementsByTagName('methodInfo') == []:
                        NOP.append('0')
		else:
                        NOP.append(sourceNode.getElementsByTagName('methodInfo')[0].getAttribute('mParaNum'))
		context = 'DEF'
		flag = 1 if sourceNode.getElementsByTagName('blockInfo') == [] else 0
		if flag == 0:
			btype_nodes = sourceNode.getElementsByTagName('bType')
			l = len(btype_nodes)
			btype_real = btype_nodes[l - 1]
			context = btype_real.childNodes[0].nodeValue
		CTX.append(context)	
	class_nodes = root1.getElementsByTagName('class')
	num = 0	
	for classNode in class_nodes:
		n = int(classNode.getAttribute('nclones'))
		similarity = int(classNode.getAttribute('similarity'))
		similarity = float(similarity) / 100
		source_nodes = classNode.getElementsByTagName('source')
		files = []		
		snum = 0
		for sourceNode in source_nodes:
			DNAME.append(sys.argv[1] + filename1 + ' ')
			class_id = sourceNode.parentNode.getAttribute('classid')
			CID.append(str(class_id) + ' ')
			snum = snum + 1		
			SID.append(str(snum))
			files.append(sourceNode.getAttribute('file'))
			same = '1' if len(set(files)) == 1 else '0'
		while n > 0:
			DBT.append(same)
			SIM.append(str(similarity))
			n = n - 1
		num = num + int(classNode.getAttribute('nclones'))
	nclones.append(num)
for i in range(1, int(nclones[0]) + 1):
	TG.append(1)
	EP.append(3)
	STATIC.append('0')
	SAME.append('0')
	ADD.append('1')
	DELETE.append('0')
	SPLIT.append('0')
	AGE.append('1')
	CC.append('0')
file_list2 = os.listdir(sys.argv[2])
for index in range(0, len(file_list2)):
	doc2 = xml.dom.minidom.parse(sys.argv[2] + file_list2[index]) 
	root2 = doc2.documentElement
	cgmap_nodes = root2.getElementsByTagName('CGMap')
	unmappeddestcg_nodes = root2.getElementsByTagName('UnMappedDestCG')
	unmappeddestcf_nodes = []
	if unmappeddestcg_nodes != []:
		unmappeddestcf_nodes = unmappeddestcg_nodes[0].getElementsByTagName('CGInfo')
	for destCGid in range(1, int(nclones[index + 1]) + 1):
		for cgmapnode in cgmap_nodes:
			if int(cgmapnode.getAttribute('destCGid')) == destCGid:
				pattern = 1
				harmful = 1
				pattern_static = '0'
				pattern_same = '0'
				pattern_add = '0'
				pattern_delete = '0' 
				pattern_split = '0'
				epnode = cgmapnode.getElementsByTagName('EvolutionPattern')[0]
				if epnode.getAttribute('STATIC') == 'True':
					pattern_static = '1'
				if epnode.getAttribute('SAME') == 'True':
					pattern_same = '1'
				if epnode.getAttribute('ADD') == 'True':
					pattern_add = '1'
				if epnode.getAttribute('DELETE') == 'True':
					pattern_delete = '1'
				if epnode.getAttribute('SPLIT') == 'True':
					pattern_split = '1'
				if epnode.getAttribute('INCONSISTENTCHANGE') == 'True':
					pattern = 3
				elif epnode.getAttribute('CONSISTENTCHANGE') == 'True':
					pattern = 2
					harmful = 0 				
				CGid = int(cgmapnode.getAttribute('srcCGid'))
				cfmap_nodes = cgmapnode.getElementsByTagName('CFMap')
				destCGsize = int(cgmapnode.getAttribute('destCGsize'))
				for destCFid in range(1, destCGsize + 1):
					EP.append(pattern)
					TG.append(harmful)
					STATIC.append(pattern_static)
					SAME.append(pattern_same)
					ADD.append(pattern_add)
					DELETE.append(pattern_delete)
					SPLIT.append(pattern_split)
					ffound = 0
					for cfmapnode in cfmap_nodes:
						if int(cfmapnode.getAttribute('destCFid')) == destCFid:
							ffound = 1
							lifetime = 1
							changetime = 0
							CFid = int(cfmapnode.getAttribute('srcCFid'))
							old_CGid = CGid
							for i in range(index - 1, -1, -1):
								doc = xml.dom.minidom.parse(sys.argv[2] + file_list2[i])
								root = doc.documentElement
								old_cgmap_nodes = root.getElementsByTagName('CGMap')
								aimCGid = old_CGid
								gfound = 0
								for old_cgmapnode in old_cgmap_nodes:
									if int(old_cgmapnode.getAttribute('destCGid')) == aimCGid:
										old_cfmap_nodes = old_cgmapnode.getElementsByTagName('CFMap')
										old_CGid = int(old_cgmapnode.getAttribute('srcCGid'))
										for old_cfmapnode in old_cfmap_nodes:
											if int(old_cfmapnode.getAttribute('destCFid')) == CFid:
												gfound = 1
												lifetime = lifetime + 1
												if old_cfmapnode.getAttribute('textSim') != '1':
													changetime = changetime + 1
												CFid = int(old_cfmapnode.getAttribute('srcCFid'))
								if gfound == 0:
									break
							AGE.append(str(lifetime))
							CC.append(str(changetime))
					if ffound == 0:
						AGE.append('1')
						CC.append('0')
		for unmappeddestcfnode in unmappeddestcf_nodes:
			if int(unmappeddestcfnode.getAttribute('id')) == destCGid:
				destCGsize = int(unmappeddestcfnode.getAttribute('size'))
				for i in range(0, destCGsize):
					TG.append(1)
					EP.append(1)
					STATIC.append('0')
					SAME.append('0')
					ADD.append('1')
					DELETE.append('0')
					SPLIT.append('0')
					AGE.append('1')
					CC.append('0')

IG_all = 0
IG_LOC = 0
IG_Hal = 0
IG_Sim = 0
IG_nParam = 0
IG_context = 0
IG_FD = 0
IG_Age = 0
IG_CC = 0
positive_num = 0
negtive_num = 0
total_num = len(TG)
uLOC = []
splitLOC = []
uTG = []
for item in TG:
        if item == 0:
                positive_num = positive_num + 1
        elif item == 1:
                negtive_num = negtive_num + 1
IG_all = - (positive_num / float(total_num)) * math.log((positive_num / float(total_num)), 2) - (negtive_num / float(total_num)) * math.log((negtive_num / float(total_num)), 2)
def IG(feature):
        unique = []
        split = []
        for item in set(feature):
                unique.append(float(item))
        unique.sort()
        for i in range(0, len(unique) - 1):
                split.append(float(unique[i] + unique[i+1])/2)
        IG_best = 100
        for i in range(0, len(split)):
                pot = split[i]
                phigh = 0
                nhigh = 0
                plow = 0
                nlow = 0
                for j in range(0, len(feature)):
                        if float(feature[j]) > pot:
                                if TG[j] == 0:
                                        phigh = phigh + 1
                                elif TG[j] == 1:
                                        nhigh = nhigh + 1
                        else:
                                if TG[j] == 0:
                                        plow = plow + 1
                                elif TG[j] == 1:
                                        nlow = nlow + 1
                thigh = phigh + nhigh
                tlow = plow + nlow
                if phigh == 0:
                        IG_phigh = 0
                else:
                        IG_phigh = -(phigh/float(thigh))*math.log((phigh/float(thigh)),2)
                if nhigh == 0:
                        IG_nhigh = 0
                else:
                        IG_nhigh = -(nhigh/float(thigh))*math.log((nhigh/float(thigh)),2)
                IG_high = thigh/float(thigh+tlow)*(IG_phigh+IG_nhigh)
                if plow == 0:
                        IG_plow = 0
                else:
                        IG_plow = -(plow/float(tlow))*math.log((plow/float(tlow)),2)
                if nlow == 0:
                        IG_nlow = 0
                else:
                        IG_nlow = -(nlow/float(tlow))*math.log((nlow/float(tlow)),2)
                IG_low = tlow/float(thigh+tlow)*(IG_plow+IG_nlow)
                IG_temp = IG_high + IG_low
                if IG_temp < IG_best:
                        IG_best = IG_temp
        return IG_best
IGCTX = []
for item in CTX:
	if item == 'DEF':
		IGCTX.append(1)
	elif item == 'IF' or item == 'ELSE' or item == 'SWITCH':
		IGCTX.append(2)
	elif item == 'FOR' or 'WHILE' or 'DO':
		IGCTX.append(3)
	else:
		IGCTX.append(4)
IG_LOC = IG_all - IG(LOC)
IG_Sim = IG_all - IG(SIM)
IG_nParam = IG_all - IG(NOP)
IG_FD = IG_all - IG(DBT)
IG_Age = IG_all - IG(AGE)
IG_CC = IG_all - IG(CC)
IG_Hal = (4*IG_all - IG(NOT) - IG(NOD) - IG(UOT) - IG(UOD))/4
IG_context = IG_all - IG(IGCTX)
print('Information Gain:')
print('+++++++++++++++++++++++++++++++')
print('1.IG of LOC             : ' + str(IG_LOC))
print('2.IG of Halstead        : ' + str(IG_Hal))
print('3.IG of Similarity      : ' + str(IG_Sim))
print('4.IG of #Params         : ' + str(IG_nParam))
print('5.IG of Context         : ' + str(IG_context))
print('6.IG of FileDistribution: ' + str(IG_FD))
print('7.IG of Age             : ' + str(IG_Age))
print('8.IG of ChangeComplexity: ' + str(IG_CC))

extracttemp_file = open('extract_temp', 'w')

extracttemp_file.write('1.IG of LOC             : ' + str(IG_LOC) + '\n')
extracttemp_file.write('2.IG of Halstead        : ' + str(IG_Hal) + '\n')
extracttemp_file.write('3.IG of Similarity      : ' + str(IG_Sim) + '\n')
extracttemp_file.write('4.IG of #Params         : ' + str(IG_nParam) + '\n')
extracttemp_file.write('5.IG of Context         : ' + str(IG_context) + '\n')
extracttemp_file.write('6.IG of FileDistribution: ' + str(IG_FD) + '\n')
extracttemp_file.write('7.IG of Age             : ' + str(IG_Age) + '\n')
extracttemp_file.write('8.IG of ChangeComplexity: ' + str(IG_CC) + '\n')
extracttemp_file.close()

LOC_for_svm = []
DBT_for_svm = []
CTX_for_svm = []
NOT_for_svm = []
NOD_for_svm = []
UOT_for_svm = []
UOD_for_svm = []
EP_for_svm  = []
AGE_for_svm = []
CC_for_svm  = []
TG_for_svm  = []
SIM_for_svm = []
NOP_for_svm = []
LEN_for_svm = []
VCB_for_svm = []
VOL_for_svm = []
LV_for_svm = []
DIF_for_svm = []
CON_for_svm = []
EFF_for_svm = []
PT_for_svm = []
STATIC_for_svm = []
SAME_for_svm = []
ADD_for_svm = []
DELETE_for_svm = []
SPLIT_for_svm = []
for item in TG:
	if item == 1:
		item = '-1 '
	elif item == 0:
		item = '+1 '
	TG_for_svm.append(item)
for item in LOC:
	item = ' 1:' + item
	LOC_for_svm.append(item)
for item in DBT:
	item = ' 2:' + item
	DBT_for_svm.append(item)
for item in CTX:
	if item == 'DEF':
		item = ' 3:1 4:0 5:0 6:0'
	elif item == 'IF' or item == 'ELSE' or item == 'SWITCH':
		item = ' 3:0 4:1 5:0 6:0'
	elif item == 'FOR' or 'WHILE' or 'DO':
		item = ' 3:0 4:0 5:1 6:0'
	else:
		item = ' 3:0 4:0 5:0 6:0'
	CTX_for_svm.append(item)
for item in NOT:
	item = ' 7:' + item
	NOT_for_svm.append(item)
for item in NOD:
	item = ' 8:' + item
	NOD_for_svm.append(item)
for item in UOT:
	item = ' 9:' + item
	UOT_for_svm.append(item)
for item in UOD:
	item = ' 10:' + item
	UOD_for_svm.append(item)
for item in AGE:
	item = ' 11:' + item
	AGE_for_svm.append(item)
for item in CC:
	item = ' 12:' + item
	CC_for_svm.append(item)
for item in SIM:
        item = ' 13:' + item
        SIM_for_svm.append(item)
for item in NOP:
        item = ' 14:' + item
        NOP_for_svm.append(item)
for item in LEN:
        item = ' 15:' + item
        LEN_for_svm.append(item)
for item in VCB:
        item = ' 16:' + item
        VCB_for_svm.append(item)
for item in VOL:
        item = ' 17:' + item
        VOL_for_svm.append(item)
for item in LV:
        item = ' 18:' + item
        LV_for_svm.append(item)
for item in DIF:
        item = ' 19:' + item
        DIF_for_svm.append(item)
for item in CON:
        item = ' 20:' + item
        CON_for_svm.append(item)
for item in EFF:
        item = ' 21:' + item
        EFF_for_svm.append(item)
for item in PT:
        item = ' 22:' + item
        PT_for_svm.append(item)
for item in STATIC:
		item = ' 23:' + item
		STATIC_for_svm.append(item)
for item in EP:
	if item == 1:
		item = ' 24:0 25:0'
	elif item == 2:
		item = ' 24:1 25:0'
	else:
		item = ' 24:0 25:1'
	EP_for_svm.append(item)
for item in SAME:
        item = ' 26:' + item
        SAME_for_svm.append(item)
for item in ADD:
        item = ' 27:' + item
        ADD_for_svm.append(item)
for item in DELETE:
        item = ' 28:' + item
        DELETE_for_svm.append(item)
for item in SPLIT:
        item = ' 29:' + item
        SPLIT_for_svm.append(item)
train_file = open('train', 'w')
test_file = open('test', 'w')
matrix_file = open('matrix','w')
zipt = zip(TG_for_svm, DNAME, CID, SID, LOC_for_svm, DBT_for_svm, CTX_for_svm, NOT_for_svm, NOD_for_svm, UOT_for_svm, UOD_for_svm,
           AGE_for_svm, CC_for_svm, SIM_for_svm, NOP_for_svm, LEN_for_svm, VCB_for_svm, VOL_for_svm, LV_for_svm, DIF_for_svm, CON_for_svm,
		   EFF_for_svm, PT_for_svm, STATIC_for_svm, EP_for_svm, SAME_for_svm, ADD_for_svm, DELETE_for_svm, SPLIT_for_svm)
t = list(zipt)
for i in range(0, len(LOC_for_svm)):
	matrix_file.writelines(t[i])
	matrix_file.write('\n')
zipped = zip(TG_for_svm, LOC_for_svm, DBT_for_svm, CTX_for_svm, NOT_for_svm, NOD_for_svm, UOT_for_svm, UOD_for_svm,
             AGE_for_svm, CC_for_svm, SIM_for_svm, NOP_for_svm, LEN_for_svm, VCB_for_svm, VOL_for_svm, LV_for_svm, DIF_for_svm, CON_for_svm,
			 EFF_for_svm, PT_for_svm, STATIC_for_svm, EP_for_svm, SAME_for_svm, ADD_for_svm, DELETE_for_svm, SPLIT_for_svm)
#zipped = zip(TG_for_svm, EP_for_svm)
k = list(zipped)
num_of_trainset = num_of_clones(0, 7)
for i in range(0, num_of_trainset):
	train_file.writelines(k[i])
	train_file.write('\n')
for i in range(num_of_trainset, len(LOC_for_svm)):
        test_file.writelines(k[i])
        test_file.write('\n')
train_file.close()
test_file.close()
matrix_file.close()
