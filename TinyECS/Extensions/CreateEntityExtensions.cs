namespace TinyECS.Extensions;

public static class CreateEntityExtensions
{
    public static EntityId CreateEntity<T1>(this World world, T1 c1)
    {
        var entity = world.CreateEntity();
        world.Add(entity, c1);
        return entity;
    }

    public static EntityId CreateEntity<T1, T2>(this World world, T1 c1, T2 c2)
    {
        var entity = world.CreateEntity();
        world.Add(entity, c1);
        world.Add(entity, c2);
        return entity;
    }

    public static EntityId CreateEntity<T1, T2, T3>(this World world, T1 c1, T2 c2, T3 c3)
    {
        var entity = world.CreateEntity();
        world.Add(entity, c1);
        world.Add(entity, c2);
        world.Add(entity, c3);
        return entity;
    }

    public static EntityId CreateEntity<T1, T2, T3, T4>(this World world, T1 c1, T2 c2, T3 c3, T4 c4)
    {
        var entity = world.CreateEntity();
        world.Add(entity, c1);
        world.Add(entity, c2);
        world.Add(entity, c3);
        world.Add(entity, c4);
        return entity;
    }

    public static EntityId CreateEntity<T1, T2, T3, T4, T5>(this World world, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5)
    {
        var entity = world.CreateEntity();
        world.Add(entity, c1);
        world.Add(entity, c2);
        world.Add(entity, c3);
        world.Add(entity, c4);
        world.Add(entity, c5);
        return entity;
    }

    public static EntityId CreateEntity<T1, T2, T3, T4, T5, T6>(this World world, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6)
    {
        var entity = world.CreateEntity();
        world.Add(entity, c1);
        world.Add(entity, c2);
        world.Add(entity, c3);
        world.Add(entity, c4);
        world.Add(entity, c5);
        world.Add(entity, c6);
        return entity;
    }

    public static EntityId CreateEntity<T1, T2, T3, T4, T5, T6, T7>(this World world, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7)
    {
        var entity = world.CreateEntity();
        world.Add(entity, c1);
        world.Add(entity, c2);
        world.Add(entity, c3);
        world.Add(entity, c4);
        world.Add(entity, c5);
        world.Add(entity, c6);
        world.Add(entity, c7);
        return entity;
    }

    public static EntityId CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8>(this World world, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8)
    {
        var entity = world.CreateEntity();
        world.Add(entity, c1);
        world.Add(entity, c2);
        world.Add(entity, c3);
        world.Add(entity, c4);
        world.Add(entity, c5);
        world.Add(entity, c6);
        world.Add(entity, c7);
        world.Add(entity, c8);
        return entity;
    }

    public static EntityId CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this World world, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9)
    {
        var entity = world.CreateEntity();
        world.Add(entity, c1);
        world.Add(entity, c2);
        world.Add(entity, c3);
        world.Add(entity, c4);
        world.Add(entity, c5);
        world.Add(entity, c6);
        world.Add(entity, c7);
        world.Add(entity, c8);
        world.Add(entity, c9);
        return entity;
    }

    public static EntityId CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this World world, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9, T10 c10)
    {
        var entity = world.CreateEntity();
        world.Add(entity, c1);
        world.Add(entity, c2);
        world.Add(entity, c3);
        world.Add(entity, c4);
        world.Add(entity, c5);
        world.Add(entity, c6);
        world.Add(entity, c7);
        world.Add(entity, c8);
        world.Add(entity, c9);
        world.Add(entity, c10);
        return entity;
    }

    public static EntityId CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(this World world, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9, T10 c10, T11 c11)
    {
        var entity = world.CreateEntity();
        world.Add(entity, c1);
        world.Add(entity, c2);
        world.Add(entity, c3);
        world.Add(entity, c4);
        world.Add(entity, c5);
        world.Add(entity, c6);
        world.Add(entity, c7);
        world.Add(entity, c8);
        world.Add(entity, c9);
        world.Add(entity, c10);
        world.Add(entity, c11);
        return entity;
    }

    public static EntityId CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(this World world, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9, T10 c10, T11 c11, T12 c12)
    {
        var entity = world.CreateEntity();
        world.Add(entity, c1);
        world.Add(entity, c2);
        world.Add(entity, c3);
        world.Add(entity, c4);
        world.Add(entity, c5);
        world.Add(entity, c6);
        world.Add(entity, c7);
        world.Add(entity, c8);
        world.Add(entity, c9);
        world.Add(entity, c10);
        world.Add(entity, c11);
        world.Add(entity, c12);
        return entity;
    }

    public static EntityId CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(this World world, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9, T10 c10, T11 c11, T12 c12, T13 c13)
    {
        var entity = world.CreateEntity();
        world.Add(entity, c1);
        world.Add(entity, c2);
        world.Add(entity, c3);
        world.Add(entity, c4);
        world.Add(entity, c5);
        world.Add(entity, c6);
        world.Add(entity, c7);
        world.Add(entity, c8);
        world.Add(entity, c9);
        world.Add(entity, c10);
        world.Add(entity, c11);
        world.Add(entity, c12);
        world.Add(entity, c13);
        return entity;
    }

    public static EntityId CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(this World world, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9, T10 c10, T11 c11, T12 c12, T13 c13, T14 c14)
    {
        var entity = world.CreateEntity();
        world.Add(entity, c1);
        world.Add(entity, c2);
        world.Add(entity, c3);
        world.Add(entity, c4);
        world.Add(entity, c5);
        world.Add(entity, c6);
        world.Add(entity, c7);
        world.Add(entity, c8);
        world.Add(entity, c9);
        world.Add(entity, c10);
        world.Add(entity, c11);
        world.Add(entity, c12);
        world.Add(entity, c13);
        world.Add(entity, c14);
        return entity;
    }

    public static EntityId CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(this World world, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9, T10 c10, T11 c11, T12 c12, T13 c13, T14 c14, T15 c15)
    {
        var entity = world.CreateEntity();
        world.Add(entity, c1);
        world.Add(entity, c2);
        world.Add(entity, c3);
        world.Add(entity, c4);
        world.Add(entity, c5);
        world.Add(entity, c6);
        world.Add(entity, c7);
        world.Add(entity, c8);
        world.Add(entity, c9);
        world.Add(entity, c10);
        world.Add(entity, c11);
        world.Add(entity, c12);
        world.Add(entity, c13);
        world.Add(entity, c14);
        world.Add(entity, c15);
        return entity;
    }

    public static EntityId CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(this World world, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9, T10 c10, T11 c11, T12 c12, T13 c13, T14 c14, T15 c15, T16 c16)
    {
        var entity = world.CreateEntity();
        world.Add(entity, c1);
        world.Add(entity, c2);
        world.Add(entity, c3);
        world.Add(entity, c4);
        world.Add(entity, c5);
        world.Add(entity, c6);
        world.Add(entity, c7);
        world.Add(entity, c8);
        world.Add(entity, c9);
        world.Add(entity, c10);
        world.Add(entity, c11);
        world.Add(entity, c12);
        world.Add(entity, c13);
        world.Add(entity, c14);
        world.Add(entity, c15);
        world.Add(entity, c16);
        return entity;
    }

    public static EntityId CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>(this World world, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9, T10 c10, T11 c11, T12 c12, T13 c13, T14 c14, T15 c15, T16 c16, T17 c17)
    {
        var entity = world.CreateEntity();
        world.Add(entity, c1);
        world.Add(entity, c2);
        world.Add(entity, c3);
        world.Add(entity, c4);
        world.Add(entity, c5);
        world.Add(entity, c6);
        world.Add(entity, c7);
        world.Add(entity, c8);
        world.Add(entity, c9);
        world.Add(entity, c10);
        world.Add(entity, c11);
        world.Add(entity, c12);
        world.Add(entity, c13);
        world.Add(entity, c14);
        world.Add(entity, c15);
        world.Add(entity, c16);
        world.Add(entity, c17);
        return entity;
    }

    public static EntityId CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18>(this World world, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9, T10 c10, T11 c11, T12 c12, T13 c13, T14 c14, T15 c15, T16 c16, T17 c17, T18 c18)
    {
        var entity = world.CreateEntity();
        world.Add(entity, c1);
        world.Add(entity, c2);
        world.Add(entity, c3);
        world.Add(entity, c4);
        world.Add(entity, c5);
        world.Add(entity, c6);
        world.Add(entity, c7);
        world.Add(entity, c8);
        world.Add(entity, c9);
        world.Add(entity, c10);
        world.Add(entity, c11);
        world.Add(entity, c12);
        world.Add(entity, c13);
        world.Add(entity, c14);
        world.Add(entity, c15);
        world.Add(entity, c16);
        world.Add(entity, c17);
        world.Add(entity, c18);
        return entity;
    }

    public static EntityId CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>(this World world, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9, T10 c10, T11 c11, T12 c12, T13 c13, T14 c14, T15 c15, T16 c16, T17 c17, T18 c18, T19 c19)
    {
        var entity = world.CreateEntity();
        world.Add(entity, c1);
        world.Add(entity, c2);
        world.Add(entity, c3);
        world.Add(entity, c4);
        world.Add(entity, c5);
        world.Add(entity, c6);
        world.Add(entity, c7);
        world.Add(entity, c8);
        world.Add(entity, c9);
        world.Add(entity, c10);
        world.Add(entity, c11);
        world.Add(entity, c12);
        world.Add(entity, c13);
        world.Add(entity, c14);
        world.Add(entity, c15);
        world.Add(entity, c16);
        world.Add(entity, c17);
        world.Add(entity, c18);
        world.Add(entity, c19);
        return entity;
    }

    public static EntityId CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20>(this World world, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9, T10 c10, T11 c11, T12 c12, T13 c13, T14 c14, T15 c15, T16 c16, T17 c17, T18 c18, T19 c19, T20 c20)
    {
        var entity = world.CreateEntity();
        world.Add(entity, c1);
        world.Add(entity, c2);
        world.Add(entity, c3);
        world.Add(entity, c4);
        world.Add(entity, c5);
        world.Add(entity, c6);
        world.Add(entity, c7);
        world.Add(entity, c8);
        world.Add(entity, c9);
        world.Add(entity, c10);
        world.Add(entity, c11);
        world.Add(entity, c12);
        world.Add(entity, c13);
        world.Add(entity, c14);
        world.Add(entity, c15);
        world.Add(entity, c16);
        world.Add(entity, c17);
        world.Add(entity, c18);
        world.Add(entity, c19);
        world.Add(entity, c20);
        return entity;
    }

    public static EntityId CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21>(this World world, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9, T10 c10, T11 c11, T12 c12, T13 c13, T14 c14, T15 c15, T16 c16, T17 c17, T18 c18, T19 c19, T20 c20, T21 c21)
    {
        var entity = world.CreateEntity();
        world.Add(entity, c1);
        world.Add(entity, c2);
        world.Add(entity, c3);
        world.Add(entity, c4);
        world.Add(entity, c5);
        world.Add(entity, c6);
        world.Add(entity, c7);
        world.Add(entity, c8);
        world.Add(entity, c9);
        world.Add(entity, c10);
        world.Add(entity, c11);
        world.Add(entity, c12);
        world.Add(entity, c13);
        world.Add(entity, c14);
        world.Add(entity, c15);
        world.Add(entity, c16);
        world.Add(entity, c17);
        world.Add(entity, c18);
        world.Add(entity, c19);
        world.Add(entity, c20);
        world.Add(entity, c21);
        return entity;
    }

    public static EntityId CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22>(this World world, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9, T10 c10, T11 c11, T12 c12, T13 c13, T14 c14, T15 c15, T16 c16, T17 c17, T18 c18, T19 c19, T20 c20, T21 c21, T22 c22)
    {
        var entity = world.CreateEntity();
        world.Add(entity, c1);
        world.Add(entity, c2);
        world.Add(entity, c3);
        world.Add(entity, c4);
        world.Add(entity, c5);
        world.Add(entity, c6);
        world.Add(entity, c7);
        world.Add(entity, c8);
        world.Add(entity, c9);
        world.Add(entity, c10);
        world.Add(entity, c11);
        world.Add(entity, c12);
        world.Add(entity, c13);
        world.Add(entity, c14);
        world.Add(entity, c15);
        world.Add(entity, c16);
        world.Add(entity, c17);
        world.Add(entity, c18);
        world.Add(entity, c19);
        world.Add(entity, c20);
        world.Add(entity, c21);
        world.Add(entity, c22);
        return entity;
    }

    public static EntityId CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23>(this World world, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9, T10 c10, T11 c11, T12 c12, T13 c13, T14 c14, T15 c15, T16 c16, T17 c17, T18 c18, T19 c19, T20 c20, T21 c21, T22 c22, T23 c23)
    {
        var entity = world.CreateEntity();
        world.Add(entity, c1);
        world.Add(entity, c2);
        world.Add(entity, c3);
        world.Add(entity, c4);
        world.Add(entity, c5);
        world.Add(entity, c6);
        world.Add(entity, c7);
        world.Add(entity, c8);
        world.Add(entity, c9);
        world.Add(entity, c10);
        world.Add(entity, c11);
        world.Add(entity, c12);
        world.Add(entity, c13);
        world.Add(entity, c14);
        world.Add(entity, c15);
        world.Add(entity, c16);
        world.Add(entity, c17);
        world.Add(entity, c18);
        world.Add(entity, c19);
        world.Add(entity, c20);
        world.Add(entity, c21);
        world.Add(entity, c22);
        world.Add(entity, c23);
        return entity;
    }

    public static EntityId CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24>(this World world, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9, T10 c10, T11 c11, T12 c12, T13 c13, T14 c14, T15 c15, T16 c16, T17 c17, T18 c18, T19 c19, T20 c20, T21 c21, T22 c22, T23 c23, T24 c24)
    {
        var entity = world.CreateEntity();
        world.Add(entity, c1);
        world.Add(entity, c2);
        world.Add(entity, c3);
        world.Add(entity, c4);
        world.Add(entity, c5);
        world.Add(entity, c6);
        world.Add(entity, c7);
        world.Add(entity, c8);
        world.Add(entity, c9);
        world.Add(entity, c10);
        world.Add(entity, c11);
        world.Add(entity, c12);
        world.Add(entity, c13);
        world.Add(entity, c14);
        world.Add(entity, c15);
        world.Add(entity, c16);
        world.Add(entity, c17);
        world.Add(entity, c18);
        world.Add(entity, c19);
        world.Add(entity, c20);
        world.Add(entity, c21);
        world.Add(entity, c22);
        world.Add(entity, c23);
        world.Add(entity, c24);
        return entity;
    }

    public static EntityId CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25>(this World world, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9, T10 c10, T11 c11, T12 c12, T13 c13, T14 c14, T15 c15, T16 c16, T17 c17, T18 c18, T19 c19, T20 c20, T21 c21, T22 c22, T23 c23, T24 c24, T25 c25)
    {
        var entity = world.CreateEntity();
        world.Add(entity, c1);
        world.Add(entity, c2);
        world.Add(entity, c3);
        world.Add(entity, c4);
        world.Add(entity, c5);
        world.Add(entity, c6);
        world.Add(entity, c7);
        world.Add(entity, c8);
        world.Add(entity, c9);
        world.Add(entity, c10);
        world.Add(entity, c11);
        world.Add(entity, c12);
        world.Add(entity, c13);
        world.Add(entity, c14);
        world.Add(entity, c15);
        world.Add(entity, c16);
        world.Add(entity, c17);
        world.Add(entity, c18);
        world.Add(entity, c19);
        world.Add(entity, c20);
        world.Add(entity, c21);
        world.Add(entity, c22);
        world.Add(entity, c23);
        world.Add(entity, c24);
        world.Add(entity, c25);
        return entity;
    }

    public static EntityId CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26>(this World world, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9, T10 c10, T11 c11, T12 c12, T13 c13, T14 c14, T15 c15, T16 c16, T17 c17, T18 c18, T19 c19, T20 c20, T21 c21, T22 c22, T23 c23, T24 c24, T25 c25, T26 c26)
    {
        var entity = world.CreateEntity();
        world.Add(entity, c1);
        world.Add(entity, c2);
        world.Add(entity, c3);
        world.Add(entity, c4);
        world.Add(entity, c5);
        world.Add(entity, c6);
        world.Add(entity, c7);
        world.Add(entity, c8);
        world.Add(entity, c9);
        world.Add(entity, c10);
        world.Add(entity, c11);
        world.Add(entity, c12);
        world.Add(entity, c13);
        world.Add(entity, c14);
        world.Add(entity, c15);
        world.Add(entity, c16);
        world.Add(entity, c17);
        world.Add(entity, c18);
        world.Add(entity, c19);
        world.Add(entity, c20);
        world.Add(entity, c21);
        world.Add(entity, c22);
        world.Add(entity, c23);
        world.Add(entity, c24);
        world.Add(entity, c25);
        world.Add(entity, c26);
        return entity;
    }

    public static EntityId CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27>(this World world, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9, T10 c10, T11 c11, T12 c12, T13 c13, T14 c14, T15 c15, T16 c16, T17 c17, T18 c18, T19 c19, T20 c20, T21 c21, T22 c22, T23 c23, T24 c24, T25 c25, T26 c26, T27 c27)
    {
        var entity = world.CreateEntity();
        world.Add(entity, c1);
        world.Add(entity, c2);
        world.Add(entity, c3);
        world.Add(entity, c4);
        world.Add(entity, c5);
        world.Add(entity, c6);
        world.Add(entity, c7);
        world.Add(entity, c8);
        world.Add(entity, c9);
        world.Add(entity, c10);
        world.Add(entity, c11);
        world.Add(entity, c12);
        world.Add(entity, c13);
        world.Add(entity, c14);
        world.Add(entity, c15);
        world.Add(entity, c16);
        world.Add(entity, c17);
        world.Add(entity, c18);
        world.Add(entity, c19);
        world.Add(entity, c20);
        world.Add(entity, c21);
        world.Add(entity, c22);
        world.Add(entity, c23);
        world.Add(entity, c24);
        world.Add(entity, c25);
        world.Add(entity, c26);
        world.Add(entity, c27);
        return entity;
    }

    public static EntityId CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28>(this World world, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9, T10 c10, T11 c11, T12 c12, T13 c13, T14 c14, T15 c15, T16 c16, T17 c17, T18 c18, T19 c19, T20 c20, T21 c21, T22 c22, T23 c23, T24 c24, T25 c25, T26 c26, T27 c27, T28 c28)
    {
        var entity = world.CreateEntity();
        world.Add(entity, c1);
        world.Add(entity, c2);
        world.Add(entity, c3);
        world.Add(entity, c4);
        world.Add(entity, c5);
        world.Add(entity, c6);
        world.Add(entity, c7);
        world.Add(entity, c8);
        world.Add(entity, c9);
        world.Add(entity, c10);
        world.Add(entity, c11);
        world.Add(entity, c12);
        world.Add(entity, c13);
        world.Add(entity, c14);
        world.Add(entity, c15);
        world.Add(entity, c16);
        world.Add(entity, c17);
        world.Add(entity, c18);
        world.Add(entity, c19);
        world.Add(entity, c20);
        world.Add(entity, c21);
        world.Add(entity, c22);
        world.Add(entity, c23);
        world.Add(entity, c24);
        world.Add(entity, c25);
        world.Add(entity, c26);
        world.Add(entity, c27);
        world.Add(entity, c28);
        return entity;
    }

    public static EntityId CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29>(this World world, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9, T10 c10, T11 c11, T12 c12, T13 c13, T14 c14, T15 c15, T16 c16, T17 c17, T18 c18, T19 c19, T20 c20, T21 c21, T22 c22, T23 c23, T24 c24, T25 c25, T26 c26, T27 c27, T28 c28, T29 c29)
    {
        var entity = world.CreateEntity();
        world.Add(entity, c1);
        world.Add(entity, c2);
        world.Add(entity, c3);
        world.Add(entity, c4);
        world.Add(entity, c5);
        world.Add(entity, c6);
        world.Add(entity, c7);
        world.Add(entity, c8);
        world.Add(entity, c9);
        world.Add(entity, c10);
        world.Add(entity, c11);
        world.Add(entity, c12);
        world.Add(entity, c13);
        world.Add(entity, c14);
        world.Add(entity, c15);
        world.Add(entity, c16);
        world.Add(entity, c17);
        world.Add(entity, c18);
        world.Add(entity, c19);
        world.Add(entity, c20);
        world.Add(entity, c21);
        world.Add(entity, c22);
        world.Add(entity, c23);
        world.Add(entity, c24);
        world.Add(entity, c25);
        world.Add(entity, c26);
        world.Add(entity, c27);
        world.Add(entity, c28);
        world.Add(entity, c29);
        return entity;
    }

    public static EntityId CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30>(this World world, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9, T10 c10, T11 c11, T12 c12, T13 c13, T14 c14, T15 c15, T16 c16, T17 c17, T18 c18, T19 c19, T20 c20, T21 c21, T22 c22, T23 c23, T24 c24, T25 c25, T26 c26, T27 c27, T28 c28, T29 c29, T30 c30)
    {
        var entity = world.CreateEntity();
        world.Add(entity, c1);
        world.Add(entity, c2);
        world.Add(entity, c3);
        world.Add(entity, c4);
        world.Add(entity, c5);
        world.Add(entity, c6);
        world.Add(entity, c7);
        world.Add(entity, c8);
        world.Add(entity, c9);
        world.Add(entity, c10);
        world.Add(entity, c11);
        world.Add(entity, c12);
        world.Add(entity, c13);
        world.Add(entity, c14);
        world.Add(entity, c15);
        world.Add(entity, c16);
        world.Add(entity, c17);
        world.Add(entity, c18);
        world.Add(entity, c19);
        world.Add(entity, c20);
        world.Add(entity, c21);
        world.Add(entity, c22);
        world.Add(entity, c23);
        world.Add(entity, c24);
        world.Add(entity, c25);
        world.Add(entity, c26);
        world.Add(entity, c27);
        world.Add(entity, c28);
        world.Add(entity, c29);
        world.Add(entity, c30);
        return entity;
    }

    public static EntityId CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31>(this World world, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9, T10 c10, T11 c11, T12 c12, T13 c13, T14 c14, T15 c15, T16 c16, T17 c17, T18 c18, T19 c19, T20 c20, T21 c21, T22 c22, T23 c23, T24 c24, T25 c25, T26 c26, T27 c27, T28 c28, T29 c29, T30 c30, T31 c31)
    {
        var entity = world.CreateEntity();
        world.Add(entity, c1);
        world.Add(entity, c2);
        world.Add(entity, c3);
        world.Add(entity, c4);
        world.Add(entity, c5);
        world.Add(entity, c6);
        world.Add(entity, c7);
        world.Add(entity, c8);
        world.Add(entity, c9);
        world.Add(entity, c10);
        world.Add(entity, c11);
        world.Add(entity, c12);
        world.Add(entity, c13);
        world.Add(entity, c14);
        world.Add(entity, c15);
        world.Add(entity, c16);
        world.Add(entity, c17);
        world.Add(entity, c18);
        world.Add(entity, c19);
        world.Add(entity, c20);
        world.Add(entity, c21);
        world.Add(entity, c22);
        world.Add(entity, c23);
        world.Add(entity, c24);
        world.Add(entity, c25);
        world.Add(entity, c26);
        world.Add(entity, c27);
        world.Add(entity, c28);
        world.Add(entity, c29);
        world.Add(entity, c30);
        world.Add(entity, c31);
        return entity;
    }

    public static EntityId CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31, T32>(this World world, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9, T10 c10, T11 c11, T12 c12, T13 c13, T14 c14, T15 c15, T16 c16, T17 c17, T18 c18, T19 c19, T20 c20, T21 c21, T22 c22, T23 c23, T24 c24, T25 c25, T26 c26, T27 c27, T28 c28, T29 c29, T30 c30, T31 c31, T32 c32)
    {
        var entity = world.CreateEntity();
        world.Add(entity, c1);
        world.Add(entity, c2);
        world.Add(entity, c3);
        world.Add(entity, c4);
        world.Add(entity, c5);
        world.Add(entity, c6);
        world.Add(entity, c7);
        world.Add(entity, c8);
        world.Add(entity, c9);
        world.Add(entity, c10);
        world.Add(entity, c11);
        world.Add(entity, c12);
        world.Add(entity, c13);
        world.Add(entity, c14);
        world.Add(entity, c15);
        world.Add(entity, c16);
        world.Add(entity, c17);
        world.Add(entity, c18);
        world.Add(entity, c19);
        world.Add(entity, c20);
        world.Add(entity, c21);
        world.Add(entity, c22);
        world.Add(entity, c23);
        world.Add(entity, c24);
        world.Add(entity, c25);
        world.Add(entity, c26);
        world.Add(entity, c27);
        world.Add(entity, c28);
        world.Add(entity, c29);
        world.Add(entity, c30);
        world.Add(entity, c31);
        world.Add(entity, c32);
        return entity;
    }

    public static EntityId CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31, T32, T33>(this World world, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9, T10 c10, T11 c11, T12 c12, T13 c13, T14 c14, T15 c15, T16 c16, T17 c17, T18 c18, T19 c19, T20 c20, T21 c21, T22 c22, T23 c23, T24 c24, T25 c25, T26 c26, T27 c27, T28 c28, T29 c29, T30 c30, T31 c31, T32 c32, T33 c33)
    {
        var entity = world.CreateEntity();
        world.Add(entity, c1);
        world.Add(entity, c2);
        world.Add(entity, c3);
        world.Add(entity, c4);
        world.Add(entity, c5);
        world.Add(entity, c6);
        world.Add(entity, c7);
        world.Add(entity, c8);
        world.Add(entity, c9);
        world.Add(entity, c10);
        world.Add(entity, c11);
        world.Add(entity, c12);
        world.Add(entity, c13);
        world.Add(entity, c14);
        world.Add(entity, c15);
        world.Add(entity, c16);
        world.Add(entity, c17);
        world.Add(entity, c18);
        world.Add(entity, c19);
        world.Add(entity, c20);
        world.Add(entity, c21);
        world.Add(entity, c22);
        world.Add(entity, c23);
        world.Add(entity, c24);
        world.Add(entity, c25);
        world.Add(entity, c26);
        world.Add(entity, c27);
        world.Add(entity, c28);
        world.Add(entity, c29);
        world.Add(entity, c30);
        world.Add(entity, c31);
        world.Add(entity, c32);
        world.Add(entity, c33);
        return entity;
    }

    public static EntityId CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31, T32, T33, T34>(this World world, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9, T10 c10, T11 c11, T12 c12, T13 c13, T14 c14, T15 c15, T16 c16, T17 c17, T18 c18, T19 c19, T20 c20, T21 c21, T22 c22, T23 c23, T24 c24, T25 c25, T26 c26, T27 c27, T28 c28, T29 c29, T30 c30, T31 c31, T32 c32, T33 c33, T34 c34)
    {
        var entity = world.CreateEntity();
        world.Add(entity, c1);
        world.Add(entity, c2);
        world.Add(entity, c3);
        world.Add(entity, c4);
        world.Add(entity, c5);
        world.Add(entity, c6);
        world.Add(entity, c7);
        world.Add(entity, c8);
        world.Add(entity, c9);
        world.Add(entity, c10);
        world.Add(entity, c11);
        world.Add(entity, c12);
        world.Add(entity, c13);
        world.Add(entity, c14);
        world.Add(entity, c15);
        world.Add(entity, c16);
        world.Add(entity, c17);
        world.Add(entity, c18);
        world.Add(entity, c19);
        world.Add(entity, c20);
        world.Add(entity, c21);
        world.Add(entity, c22);
        world.Add(entity, c23);
        world.Add(entity, c24);
        world.Add(entity, c25);
        world.Add(entity, c26);
        world.Add(entity, c27);
        world.Add(entity, c28);
        world.Add(entity, c29);
        world.Add(entity, c30);
        world.Add(entity, c31);
        world.Add(entity, c32);
        world.Add(entity, c33);
        world.Add(entity, c34);
        return entity;
    }

    public static EntityId CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31, T32, T33, T34, T35>(this World world, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9, T10 c10, T11 c11, T12 c12, T13 c13, T14 c14, T15 c15, T16 c16, T17 c17, T18 c18, T19 c19, T20 c20, T21 c21, T22 c22, T23 c23, T24 c24, T25 c25, T26 c26, T27 c27, T28 c28, T29 c29, T30 c30, T31 c31, T32 c32, T33 c33, T34 c34, T35 c35)
    {
        var entity = world.CreateEntity();
        world.Add(entity, c1);
        world.Add(entity, c2);
        world.Add(entity, c3);
        world.Add(entity, c4);
        world.Add(entity, c5);
        world.Add(entity, c6);
        world.Add(entity, c7);
        world.Add(entity, c8);
        world.Add(entity, c9);
        world.Add(entity, c10);
        world.Add(entity, c11);
        world.Add(entity, c12);
        world.Add(entity, c13);
        world.Add(entity, c14);
        world.Add(entity, c15);
        world.Add(entity, c16);
        world.Add(entity, c17);
        world.Add(entity, c18);
        world.Add(entity, c19);
        world.Add(entity, c20);
        world.Add(entity, c21);
        world.Add(entity, c22);
        world.Add(entity, c23);
        world.Add(entity, c24);
        world.Add(entity, c25);
        world.Add(entity, c26);
        world.Add(entity, c27);
        world.Add(entity, c28);
        world.Add(entity, c29);
        world.Add(entity, c30);
        world.Add(entity, c31);
        world.Add(entity, c32);
        world.Add(entity, c33);
        world.Add(entity, c34);
        world.Add(entity, c35);
        return entity;
    }

    public static EntityId CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31, T32, T33, T34, T35, T36>(this World world, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9, T10 c10, T11 c11, T12 c12, T13 c13, T14 c14, T15 c15, T16 c16, T17 c17, T18 c18, T19 c19, T20 c20, T21 c21, T22 c22, T23 c23, T24 c24, T25 c25, T26 c26, T27 c27, T28 c28, T29 c29, T30 c30, T31 c31, T32 c32, T33 c33, T34 c34, T35 c35, T36 c36)
    {
        var entity = world.CreateEntity();
        world.Add(entity, c1);
        world.Add(entity, c2);
        world.Add(entity, c3);
        world.Add(entity, c4);
        world.Add(entity, c5);
        world.Add(entity, c6);
        world.Add(entity, c7);
        world.Add(entity, c8);
        world.Add(entity, c9);
        world.Add(entity, c10);
        world.Add(entity, c11);
        world.Add(entity, c12);
        world.Add(entity, c13);
        world.Add(entity, c14);
        world.Add(entity, c15);
        world.Add(entity, c16);
        world.Add(entity, c17);
        world.Add(entity, c18);
        world.Add(entity, c19);
        world.Add(entity, c20);
        world.Add(entity, c21);
        world.Add(entity, c22);
        world.Add(entity, c23);
        world.Add(entity, c24);
        world.Add(entity, c25);
        world.Add(entity, c26);
        world.Add(entity, c27);
        world.Add(entity, c28);
        world.Add(entity, c29);
        world.Add(entity, c30);
        world.Add(entity, c31);
        world.Add(entity, c32);
        world.Add(entity, c33);
        world.Add(entity, c34);
        world.Add(entity, c35);
        world.Add(entity, c36);
        return entity;
    }

    public static EntityId CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31, T32, T33, T34, T35, T36, T37>(this World world, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9, T10 c10, T11 c11, T12 c12, T13 c13, T14 c14, T15 c15, T16 c16, T17 c17, T18 c18, T19 c19, T20 c20, T21 c21, T22 c22, T23 c23, T24 c24, T25 c25, T26 c26, T27 c27, T28 c28, T29 c29, T30 c30, T31 c31, T32 c32, T33 c33, T34 c34, T35 c35, T36 c36, T37 c37)
    {
        var entity = world.CreateEntity();
        world.Add(entity, c1);
        world.Add(entity, c2);
        world.Add(entity, c3);
        world.Add(entity, c4);
        world.Add(entity, c5);
        world.Add(entity, c6);
        world.Add(entity, c7);
        world.Add(entity, c8);
        world.Add(entity, c9);
        world.Add(entity, c10);
        world.Add(entity, c11);
        world.Add(entity, c12);
        world.Add(entity, c13);
        world.Add(entity, c14);
        world.Add(entity, c15);
        world.Add(entity, c16);
        world.Add(entity, c17);
        world.Add(entity, c18);
        world.Add(entity, c19);
        world.Add(entity, c20);
        world.Add(entity, c21);
        world.Add(entity, c22);
        world.Add(entity, c23);
        world.Add(entity, c24);
        world.Add(entity, c25);
        world.Add(entity, c26);
        world.Add(entity, c27);
        world.Add(entity, c28);
        world.Add(entity, c29);
        world.Add(entity, c30);
        world.Add(entity, c31);
        world.Add(entity, c32);
        world.Add(entity, c33);
        world.Add(entity, c34);
        world.Add(entity, c35);
        world.Add(entity, c36);
        world.Add(entity, c37);
        return entity;
    }

    public static EntityId CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31, T32, T33, T34, T35, T36, T37, T38>(this World world, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9, T10 c10, T11 c11, T12 c12, T13 c13, T14 c14, T15 c15, T16 c16, T17 c17, T18 c18, T19 c19, T20 c20, T21 c21, T22 c22, T23 c23, T24 c24, T25 c25, T26 c26, T27 c27, T28 c28, T29 c29, T30 c30, T31 c31, T32 c32, T33 c33, T34 c34, T35 c35, T36 c36, T37 c37, T38 c38)
    {
        var entity = world.CreateEntity();
        world.Add(entity, c1);
        world.Add(entity, c2);
        world.Add(entity, c3);
        world.Add(entity, c4);
        world.Add(entity, c5);
        world.Add(entity, c6);
        world.Add(entity, c7);
        world.Add(entity, c8);
        world.Add(entity, c9);
        world.Add(entity, c10);
        world.Add(entity, c11);
        world.Add(entity, c12);
        world.Add(entity, c13);
        world.Add(entity, c14);
        world.Add(entity, c15);
        world.Add(entity, c16);
        world.Add(entity, c17);
        world.Add(entity, c18);
        world.Add(entity, c19);
        world.Add(entity, c20);
        world.Add(entity, c21);
        world.Add(entity, c22);
        world.Add(entity, c23);
        world.Add(entity, c24);
        world.Add(entity, c25);
        world.Add(entity, c26);
        world.Add(entity, c27);
        world.Add(entity, c28);
        world.Add(entity, c29);
        world.Add(entity, c30);
        world.Add(entity, c31);
        world.Add(entity, c32);
        world.Add(entity, c33);
        world.Add(entity, c34);
        world.Add(entity, c35);
        world.Add(entity, c36);
        world.Add(entity, c37);
        world.Add(entity, c38);
        return entity;
    }

    public static EntityId CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31, T32, T33, T34, T35, T36, T37, T38, T39>(this World world, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9, T10 c10, T11 c11, T12 c12, T13 c13, T14 c14, T15 c15, T16 c16, T17 c17, T18 c18, T19 c19, T20 c20, T21 c21, T22 c22, T23 c23, T24 c24, T25 c25, T26 c26, T27 c27, T28 c28, T29 c29, T30 c30, T31 c31, T32 c32, T33 c33, T34 c34, T35 c35, T36 c36, T37 c37, T38 c38, T39 c39)
    {
        var entity = world.CreateEntity();
        world.Add(entity, c1);
        world.Add(entity, c2);
        world.Add(entity, c3);
        world.Add(entity, c4);
        world.Add(entity, c5);
        world.Add(entity, c6);
        world.Add(entity, c7);
        world.Add(entity, c8);
        world.Add(entity, c9);
        world.Add(entity, c10);
        world.Add(entity, c11);
        world.Add(entity, c12);
        world.Add(entity, c13);
        world.Add(entity, c14);
        world.Add(entity, c15);
        world.Add(entity, c16);
        world.Add(entity, c17);
        world.Add(entity, c18);
        world.Add(entity, c19);
        world.Add(entity, c20);
        world.Add(entity, c21);
        world.Add(entity, c22);
        world.Add(entity, c23);
        world.Add(entity, c24);
        world.Add(entity, c25);
        world.Add(entity, c26);
        world.Add(entity, c27);
        world.Add(entity, c28);
        world.Add(entity, c29);
        world.Add(entity, c30);
        world.Add(entity, c31);
        world.Add(entity, c32);
        world.Add(entity, c33);
        world.Add(entity, c34);
        world.Add(entity, c35);
        world.Add(entity, c36);
        world.Add(entity, c37);
        world.Add(entity, c38);
        world.Add(entity, c39);
        return entity;
    }

    public static EntityId CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31, T32, T33, T34, T35, T36, T37, T38, T39, T40>(this World world, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9, T10 c10, T11 c11, T12 c12, T13 c13, T14 c14, T15 c15, T16 c16, T17 c17, T18 c18, T19 c19, T20 c20, T21 c21, T22 c22, T23 c23, T24 c24, T25 c25, T26 c26, T27 c27, T28 c28, T29 c29, T30 c30, T31 c31, T32 c32, T33 c33, T34 c34, T35 c35, T36 c36, T37 c37, T38 c38, T39 c39, T40 c40)
    {
        var entity = world.CreateEntity();
        world.Add(entity, c1);
        world.Add(entity, c2);
        world.Add(entity, c3);
        world.Add(entity, c4);
        world.Add(entity, c5);
        world.Add(entity, c6);
        world.Add(entity, c7);
        world.Add(entity, c8);
        world.Add(entity, c9);
        world.Add(entity, c10);
        world.Add(entity, c11);
        world.Add(entity, c12);
        world.Add(entity, c13);
        world.Add(entity, c14);
        world.Add(entity, c15);
        world.Add(entity, c16);
        world.Add(entity, c17);
        world.Add(entity, c18);
        world.Add(entity, c19);
        world.Add(entity, c20);
        world.Add(entity, c21);
        world.Add(entity, c22);
        world.Add(entity, c23);
        world.Add(entity, c24);
        world.Add(entity, c25);
        world.Add(entity, c26);
        world.Add(entity, c27);
        world.Add(entity, c28);
        world.Add(entity, c29);
        world.Add(entity, c30);
        world.Add(entity, c31);
        world.Add(entity, c32);
        world.Add(entity, c33);
        world.Add(entity, c34);
        world.Add(entity, c35);
        world.Add(entity, c36);
        world.Add(entity, c37);
        world.Add(entity, c38);
        world.Add(entity, c39);
        world.Add(entity, c40);
        return entity;
    }

}
/* generation code (linqpad script)
void Main()
{
	var code = Generate(30);
	code.Dump();
}

// You can define other methods, fields, classes and namespaces here
private static string Generate(int n)
{
	var sb = new StringBuilder();
	
	var functionTemplate = @"
public static EntityId CreateEntity<[[generics]]>(this World world, [[components]])
{
    var entity = world.CreateEntity();
    [[addcomponents]]
    return entity;
}
";
	var genericTemplate = @"T[[id]]";
	var componentTemplate = @"T[[id]] c[[id]]";
	var addComponentTemplate = @"world.Add(entity, c[[id]]);";

	for(int i = 0; i < n; i++)
	{
		var generics = string.Join(", ", Enumerable.Range(1, i+1).Select(x => genericTemplate.Replace("[[id]]", x.ToString())));
		var components = string.Join(", ", Enumerable.Range(1, i+1).Select(x => componentTemplate.Replace("[[id]]", x.ToString())));
		var addComponents = string.Join(Environment.NewLine+"    ", Enumerable.Range(1, i+1).Select(x => addComponentTemplate.Replace("[[id]]", x.ToString())));
		var function = functionTemplate
			.Replace("[[generics]]", generics)
			.Replace("[[components]]", components)
			.Replace("[[addcomponents]]", addComponents);
		sb.Append(function);
	}
	
	return sb.ToString();
}
*/
