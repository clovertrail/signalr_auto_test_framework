import sys

import argparse
import os

def render(tpl_path, dir_):
    path, filename = os.path.split(tpl_path)
    
    links = "";
    dirs = [ x[0] for x in os.walk(dir_)]
    for i,x in enumerate(dirs):
        x = x.replace('\\', '/')
        if i == 0: continue
        link = x.split('/')[-1]
        links += """\n                          <li><a href='{link}/index.html'>{link} 1s latency</a><div id='{link}_1s_percent_table_div'></div></li> \n""".format(link=link)
    context = None
    with open(tpl_path) as f:
        context = f.read()
        context = context.replace("[placeholder]", links);
    return context

def load_render(tpl_path, dir_):
    
    print (render(tpl_path,  dir_))
    # return render(tpl_path, dir_)


if __name__=="__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument('-t', action='store', dest='tmpl',
                        help='Specify the template file path')
    parser.add_argument('-d', action='store', dest='dir',
                        help='Specify the counter dir path')
    results = parser.parse_args()
    if results.tmpl != None:
        load_render(results.tmpl, results.dir)
        # with open ('index.html', 'w') as f:
        #     f.write(load_render(results.tmpl, results.dir))
    else:
        print ("Please specify template path")