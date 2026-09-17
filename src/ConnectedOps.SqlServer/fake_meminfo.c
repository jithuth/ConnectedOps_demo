#define _GNU_SOURCE
#include <sys/sysinfo.h>
#include <dlfcn.h>
#include <stddef.h>

/*
 * Intercepts sysinfo() to report >=4GB RAM so that Microsoft SQL Server 2022's
 * hardcoded 2000MB startup check passes on AWS Free Tier (t2.micro / t3.micro).
 * The OS swap space handles memory paging transparently.
 */
int sysinfo(struct sysinfo *info) {
    static int (*orig_sysinfo)(struct sysinfo *) = NULL;
    if (!orig_sysinfo) {
        orig_sysinfo = (int (*)(struct sysinfo *))dlsym(RTLD_NEXT, "sysinfo");
    }
    int ret = orig_sysinfo ? orig_sysinfo(info) : 0;
    if (info != NULL) {
        unsigned long unit = info->mem_unit ? info->mem_unit : 1;
        if (info->totalram < (4096UL * 1024 * 1024 / unit)) {
            info->totalram = 4096UL * 1024 * 1024 / unit;
        }
        if (info->freeram < (2048UL * 1024 * 1024 / unit)) {
            info->freeram = 2048UL * 1024 * 1024 / unit;
        }
    }
    return ret;
}
